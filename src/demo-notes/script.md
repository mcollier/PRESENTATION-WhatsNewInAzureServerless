# My Demo Notes

##### Pre-reqs
- Open Visual Studio 2017 to `AzureServerless.sln`
- Open Azure Storage Explorer
  - Copy sample images to storage account:
    - Storage Account: mcollierascend
    - Container Name: imageanalysis
- Open Windows Explorer to C:\temp\demo-pics
- Create https://requestb.in/ endpoints

## 1 - Azure Functions
### Show a basic C# Hello World
- `HelloWorldFunction.cs`
- Run in Visual Studio
- Show running via browser


### Show Image Analyzer
- `ImageAnalyzerFunction.cs`
- Walk through code
  - Discuss Azure Key Vault and Managed Service Identity
- Show version running in Azure
  - Place sample JSON into message queue:
    - Storage Account: mcollierascend
    - Queue Name: images
  - Show results in Cosmos DB:
    - Account: mcollierascend
    - Database: ImageAnalysis
    - Collection: Ascend

#### Sample JSON
```JSON
{
    "PersonName": "Michael Collier",
    "BlobName":"michael.jpg"
}
```

```JSON
{
    "PersonName": "sonjaandmike",
    "BlobName":"stlucia.jpg"
}
```

## 2 - Logic Apps
### Show `mcollier-ascend-image-logic` Logic App in Azure Portal
1. Go to 'Edit' tab to see workflow. Discuss.
    - Does mostly the same thing as the Azure Function dicussed early. However, instead of polling on a queue and looking for blobs, it is watching for blob modifications and retrieving the content directly.
    - Analyzes the image
    - Sends a text message containing the image analysis caption
    - In parallel, generates and saves a thumbnail version of the original image.
2. Add an image to the storage account:
   - Storage Account: mcollierascend
   - Container Name: my-images
3. Wait for Logic App to start (should be a few seconds)
4. Wait for SMS message (ding!)
5. Show completed run
   - Show image analysis
   - Show thumbnail blob:
     - Container Name: my-images-thumbnails


## 3 - Event Grid

### Custom Topic and Subscription
1. Show event-grid.azcli
   - Topic and subscriptions should already be created.
2. Run `EventGrid` project
   - `SendToEventGrid`
   - `SendToEventGridWithSdk`
3. Show results at https://requestb.in/

### Show Azure Storage blob subscription
1. Show results at https://requestb.in/ for all the storage events created from **mcollierascend** storage account.
   - https://requestb.in/tz40rntz?inspect

## 4 - Final Demo - Customer Car Reviews