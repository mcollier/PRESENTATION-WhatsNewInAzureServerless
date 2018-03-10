
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public static class Function1
    {
        private static DocumentClient documentClient;

        [FunctionName("HelloWorld")]
        public static IActionResult HelloWorld([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("ImageAnalyzer")]
        public static async Task DoWork(
            [QueueTrigger("%input-queue%")] InfoMessage infoMsg,
            [Blob("%input-container%/{BlobName}", FileAccess.ReadWrite)] CloudBlockBlob inputBlob,
            ILogger log, ExecutionContext context)
        {
            try
            {
                log.LogInformation($"Processing input blob {infoMsg.BlobName}.");

                var sasBlobUri = GetImageSharedAccessSignature(infoMsg.BlobName, inputBlob);

                var analysisResult = AnalyzeImage(sasBlobUri, log);
                
                await SaveImageAnalysis(infoMsg, inputBlob, analysisResult.Result, log);
            }
            catch (Exception e)
            {
                log.LogError(e, "Unable to process image!");
                throw;
            }
        }

        private static async Task SaveImageAnalysis(InfoMessage infoMsg, CloudBlockBlob myBlob, AnalysisResult analysisResult, ILogger log)
        {
            log.LogInformation("Saving image analysis.");

            string dbName = Environment.GetEnvironmentVariable("documentDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("documentCollectionName");

            if (documentClient == null)
            {
                AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient kvClient =
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                var key = await kvClient.GetSecretAsync("https://mcollier-ascend-vault.vault.azure.net/secrets/CosmosDbKey").ConfigureAwait(false);
                documentClient = new DocumentClient(new Uri("https://mcollierascend.documents.azure.com:443/"), key.Value);
            }

            await documentClient.CreateDatabaseIfNotExistsAsync(new Database {Id = dbName});
            await documentClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(dbName), new DocumentCollection {Id = collectionName},
                new RequestOptions {OfferThroughput = 400});

            ImageInfo imageInfo = new ImageInfo
            {
                Id = Guid.NewGuid().ToString(),
                ImagePath = myBlob.Uri.ToString(),
                Analysis = analysisResult
            };

            await documentClient.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(dbName, collectionName), imageInfo);
        }

        private static async Task<AnalysisResult> AnalyzeImage(string imageUrl, ILogger log)
        {
            log.LogInformation($"Starting to analyze image with Computer Vision API.");

            AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient kvClient =
                new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            var visionApiKey = await kvClient.GetSecretAsync("https://mcollier-ascend-vault.vault.azure.net/secrets/VisionApiKey").ConfigureAwait(false);
            var visionApiRegion = await kvClient.GetSecretAsync("https://mcollier-ascend-vault.vault.azure.net/secrets/VisionApiRegion").ConfigureAwait(false);

            var visionClient = new VisionServiceClient(visionApiKey.Value, visionApiRegion.Value);
            var features = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Description };
            var analysisResult = await visionClient.AnalyzeImageAsync(imageUrl, features);

            return analysisResult;
        }
        
        private static string GetImageSharedAccessSignature(string blobName, CloudBlockBlob myBlob)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(15),
                Permissions = SharedAccessBlobPermissions.Read
            };

            string token = myBlob.GetSharedAccessSignature(sasConstraints);
            return myBlob.Uri + token;
        }

        private static Uri GetFullBlobUri(string blobName)
        {
            var blockBlob = GetBlobReference(blobName);

            return blockBlob.Uri;
        }

        private static CloudBlockBlob GetBlobReference(string blobName)
        {
            string cloudStorageAccountString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string inputStorageContainerName = Environment.GetEnvironmentVariable("input-container");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(cloudStorageAccountString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(inputStorageContainerName);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);
            return blockBlob;
        }
    }

    public class ImageInfo
    {
        public string Id { get; set; }
        public string ImagePath { get; set; }
        public AnalysisResult Analysis { get; set; }
    }

    public class InfoMessage
    {
        public string PersonName { get; set; }    
        public string BlobName { get; set; }
    }
}
