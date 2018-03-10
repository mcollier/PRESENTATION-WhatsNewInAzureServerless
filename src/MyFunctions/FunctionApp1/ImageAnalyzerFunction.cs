
using FunctionApp1.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public static class ImageAnalyzerFunction
    {
        private static DocumentClient documentClient;
        private static string VisionApiKey;
        private static string VisionApiRegion;

        private static readonly string VisionApiKeySecretUri = Environment.GetEnvironmentVariable("computerVisionApiKeySecretUri");
        private static readonly string VisionApiRegionSecretUri = Environment.GetEnvironmentVariable("computerVisionApiRegionSecretUri");

        [FunctionName("ImageAnalyzer")]
        public static async Task AnalyzeImage(
            [QueueTrigger("%input-queue%")] InfoMessage infoMsg,
            [Blob("%input-container%/{BlobName}", FileAccess.ReadWrite)] CloudBlockBlob inputBlob,
            ILogger log, ExecutionContext context)
        {
            try
            {
                log.LogInformation($"Processing input blob {infoMsg.BlobName}.");

                // Create a Shared Access Signature (SAS) for the image. The SAS will be
                // used with the Computer Vision API for image analysis.
                var sasBlobUri = GetImageSharedAccessSignature(inputBlob);

                // Use Computer Vision API to analyze the image.
                var analysisResult = AnalyzeImage(sasBlobUri, log);

                // Save the results to a database.
                await SaveImageAnalysis(analysisResult.Result, inputBlob.Uri.ToString(), log);
            }
            catch (Exception e)
            {
                log.LogError(e, "Unable to process image!");
                throw;
            }
        }

        private static async Task SaveImageAnalysis(AnalysisResult analysisResult, string blobUri, ILogger log)
        {
            // NOTE: Could also use Azure Function bindings. However, the approach shown below demonstrates a nice
            //       way to use Azure Key Vault and a static DocumentClient.

            log.LogInformation("Saving image analysis.");

            string dbName = Environment.GetEnvironmentVariable("documentDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("documentCollectionName");
            string dbKeySecretUri = Environment.GetEnvironmentVariable("cosmosDbAuthKeySecretUri");
            string cosmosDbUri = Environment.GetEnvironmentVariable("cosmosDbUri");

            if (documentClient == null)
            {
                // Use Azure Managed Service Identity to authenticate with Azure Key Vault.
                AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient kvClient =
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                var key = await kvClient.GetSecretAsync(dbKeySecretUri).ConfigureAwait(false);
                documentClient = new DocumentClient(new Uri(cosmosDbUri), key.Value);
            }

            await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = dbName });
            await documentClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(dbName), new DocumentCollection { Id = collectionName },
                new RequestOptions { OfferThroughput = 400 });

            ImageInfo imageInfo = new ImageInfo
            {
                Id = Guid.NewGuid().ToString(),
                ImagePath = blobUri,
                Analysis = analysisResult
            };

            await documentClient.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(dbName, collectionName), imageInfo);
        }

        private static async Task<AnalysisResult> AnalyzeImage(string imageUrl, ILogger log)
        {
            log.LogInformation($"Starting to analyze image with Computer Vision API.");

            // Use Azure Managed Service Identity to authenticate with Azure Key Vault.
            AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient kvClient =
                new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

            if (VisionApiKey == null)
            {
                var apiKeySecretBundle = await kvClient.GetSecretAsync(VisionApiKeySecretUri).ConfigureAwait(false);
                VisionApiKey = apiKeySecretBundle.Value;
            }

            if (VisionApiRegion == null)
            {
                var apiRegionSecretBundle = await kvClient.GetSecretAsync(VisionApiRegionSecretUri).ConfigureAwait(false);
                VisionApiRegion = apiRegionSecretBundle.Value;
            }

            var visionClient = new VisionServiceClient(VisionApiKey, VisionApiRegion);
            var features = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Description };
            var analysisResult = await visionClient.AnalyzeImageAsync(imageUrl, features);

            return analysisResult;
        }

        private static string GetImageSharedAccessSignature(CloudBlob myBlob)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10),
                Permissions = SharedAccessBlobPermissions.Read
            };

            string token = myBlob.GetSharedAccessSignature(sasConstraints);
            return myBlob.Uri + token;
        }
    }
}
