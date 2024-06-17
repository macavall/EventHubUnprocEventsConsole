This is an example of the `appsettings.json` file that will be located in the root directory

``` json
{
  "EventHubConnectionString": "Endpoint=sb://eventhub566ns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<STORAGE_ACCOUNT_KEY>",
  "EventHubName": "eventhub1test2",
  "StorageEventHubContainer": "azure-webjobs-eventhub",
  "BlobServiceEndpoint": "https://<NAME_OF_STORAGE_ACCOUNT_FOR_FUNCTION_APP>.blob.core.windows.net/",
  "StorageAccountName": "<STORAGE_ACCOUNT_NAME>",
  "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=<STORAGE_ACCOUNT_NAME>;AccountKey=<STORAGE_ACCOUNT_KEY>;EndpointSuffix=core.windows.net",
  "consumerGroup": "$Default"
}
```
