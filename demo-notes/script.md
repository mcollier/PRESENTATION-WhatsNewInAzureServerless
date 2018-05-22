# My Demo Notes

##### Pre-reqs
- Open Visual Studio 2017 to `AzureServerless.sln`
- Open Azure Storage Explorer
  - Copy sample images to storage account:
    - Storage Account: cbusimages
    - Container Name: images
- Open Windows Explorer to C:\temp\demo-pics


## 1 - Azure Functions
### Show a basic C# Hello World
- `HelloWorldFunction.cs`
- Run in Visual Studio Code
- Show running via browser
    - Local:
    - Azure: https://cbusfunimages.azurewebsites.net/api/HelloWorld?code=xxxxx==&name=testmenow


### Show Image Analyzer
- `ImageAnalyzerFunction.cs`
- Walk through code
  - Discuss Azure Key Vault and Managed Service Identity
- Show version running in Azure
  - Place sample JSON into message queue:
    - Storage Account: cbusimages
    - Blob Container Name: images
    - Queue Name: images
  - Show results in Cosmos DB:
    - Account: cbusfun
    - Database: ImageAnalysis
    - Collection: images

#### Sample JSON
```JSON
{
    "BlobName":"IMG_3827.JPG"
}

{
    "BlobName":"20170702_204605547_iOS.jpg"
}

{
    "BlobName":"Shelby-GT500-GT500-SuperSwap.jpeg"
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
   - Storage Account: cbusimages
   - Container Name: images2
3. Wait for Logic App to start (should be a few seconds)
4. Wait for Twitter message (http://twitter.com/michaelcollier) 
5. Show completed run
   - Show image analysis
   - Show thumbnail blob:
     - Container Name: images2-thumbnails


## 3 - Event Grid

### Custom Topic and Subscription
1. Show event-grid.azcli
   - Topic and subscriptions should already be created.
2. Run `EventGrid` project
   - `SendToEventGrid`
   - `SendToEventGridWithSdk`
   - `SendCustomEvent`
3. Show Azure Function (in portal) that will handle validation and sending a text message via Twilio.
   - gab2018-events

### Create new Logic App based on Azure Blob Storage event (via Event Grid)
  - Compose a variable
      - split(triggerBody()?['subject'], '/')[4]/split(triggerBody()?['subject'], '/')[6]
  - Get blob content
      - Output (variable output)
  - Detect Sentiment
      - FileContent
      - Connection Info:
          - key: ***GET-THE-KEY***
          - url: https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0
  - Send email via Outlook
      - If score >= 0.5 send good email
      - else send bad email


## 4 - Final Demo - Customer Car Reviews (** OPTIONAL **)
https://gab18siteproxy.azurewebsites.net/dashboard

