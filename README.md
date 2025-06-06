## 🔐 User Secrets Configuration

To run this application locally, you’ll need to set up [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) with the following keys:

### Example `secrets.json`

```json
{
  "SearchServiceEndPoint": "https://your-endpoint.search.windows.net",
  "SearchApiKey": "",
  "AzureOpenAIEndpoint": "https://your-endpoint.cognitiveservices.azure.com/",
  "AzureOpenAIKey": "",
  "SearchIndexName": "default",
  "EmbeddingDeploymentName": "text-embedding-ada-002",
  "ChatDeploymentName": "gpt-4o",
  "ConfluenceOrg": "your-org"
}
