# My Demo Notes

##### Pre-reqs
- Open Visual Studio 2017 to `AzureServerless.sln`
- Open Azure Storage Explorer
  - Copy sample images to storage account:
    - Storage Account: gab2018fun
    - Container Name: imageanalysis
- Open Windows Explorer to C:\temp\demo-pics


## 1 - Azure Functions
### Show a basic C# Hello World
- `HelloWorldFunction.cs`
- Run in Visual Studio
- Show running via browser
    - http://localhost:7071/api/HelloWorld?name=michael
    - https://gab18fun.azurewebsites.net/api/HelloWorld?name=Columbus%20Global%20Azure%20Bootcamp&code=kr5tCathLgf/455elqlByOI/s4E42a3PaMaypWDqmSP8TfuUlhGHnw==


### Show Image Analyzer
- `ImageAnalyzerFunction.cs`
- Walk through code
  - Discuss Azure Key Vault and Managed Service Identity
- Show version running in Azure
  - Place sample JSON into message queue:
    - Storage Account: gab18fun
    - Queue Name: images
  - Show results in Cosmos DB:
    - Account: gab18fun
    - Database: ImageAnalysis
    - Collection: gab2018

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
### Show `ImageAnalysis` Logic App in Azure Portal
1. Go to 'Edit' tab to see workflow. Discuss.
    - Does mostly the same thing as the Azure Function dicussed early. However, instead of polling on a queue and looking for blobs, it is watching for blob modifications and retrieving the content directly.
    - Analyzes the image
    - Sends a Twitter message containing the image analysis caption
    - In parallel, generates and saves a thumbnail version of the original image.
2. Add an image to the storage account:
   - Storage Account: gab18images
   - Container Name: my-images
3. Wait for Logic App to start (should be a few seconds)
4. Wait for Twitter message (http://twitter.com/michaelcollier) 
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
3. Show Azure Function (in portal) that will handle validation and sending a text message via Twilio.
   - gab2018-events

### Show Azure Storage blob subscription
1. Show 'gab18events' which is a Logic App as an Event Grid subscripition on the 'gab18events' storage account.
2. When a blob is created the image is analyzed by Computer Vision API, a text/SMS message is sent, and message posted to Slack.
3. When a blob is deleted, a message is posted to Slack.

## 4 - Final Demo - Customer Car Reviews
https://gab18siteproxy.azurewebsites.net/dashboard

