using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using RagConfluenceDotNet.Helpers;
using RagConfluenceDotNet.Models;
using SharpToken;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// config

string searchServiceEndPoint = config["SearchServiceEndPoint"];
string searchApiKey = config["SearchApiKey"];
string searchIndexName = config["SearchIndexName"];

string azureOpenAIEndpoint = config["AzureOpenAIEndpoint"];
string azureOpenAIKey = config["AzureOpenAIKey"];

string embeddingDeploymentName = config["EmbeddingDeploymentName"];
string chatDeploymentName = config["ChatDeploymentName"];

string confluenceOrg = config["ConfluenceOrg"];

// end config

// Generate embedding vector from file
var azureOpenAIClient = new AzureOpenAIClient(
    new Uri(azureOpenAIEndpoint),
    new AzureKeyCredential(azureOpenAIKey)
);
var embeddingClient = azureOpenAIClient.GetEmbeddingClient(embeddingDeploymentName);

// Index the file in Azure AI Search
var searchClient = new SearchClient(
    new Uri(searchServiceEndPoint),
    searchIndexName,
    new AzureKeyCredential(searchApiKey)
);

var searchIndexClient = new SearchIndexClient(
    new Uri(searchServiceEndPoint),
    new AzureKeyCredential(searchApiKey)
);

Console.Write("Run Confluence indexing? (y/n): ");
var input = Console.ReadLine();

if (input?.Trim().ToLower() == "y")
{
    // Define index schema
    var index = new SearchIndex(searchIndexName)
    {
        Fields =
        {
            new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SearchableField("content") { IsFilterable = false, IsSortable = false },
            new SimpleField("originalUrl", SearchFieldDataType.String)
            {
                IsFilterable = false,
                IsSortable = false,
            },
            new SimpleField("chunkIndex", SearchFieldDataType.Int32)
            {
                IsFilterable = false,
                IsSortable = false,
            },
            new SearchField(
                "contentVector",
                SearchFieldDataType.Collection(SearchFieldDataType.Single)
            )
            {
                IsSearchable = true,
                VectorSearchDimensions = 1536, // Azure OpenAI embedding dimension (e.g., text-embedding-ada-002 is typically 1536)
                VectorSearchProfileName = "my-vector-profile",
            },
        },
        VectorSearch = new VectorSearch
        {
            Profiles = { new VectorSearchProfile("my-vector-profile", "my-vector-config") },
            Algorithms = { new HnswAlgorithmConfiguration("my-vector-config") },
        },
    };

    await searchIndexClient.DeleteIndexAsync(searchIndexName);
    await searchIndexClient.CreateIndexAsync(index);

    var confluenceData = await GetPublicConfluenceData();
    var docs = new List<object>();
    var tokenizer = GptEncoding.GetEncodingForModel(embeddingDeploymentName);

    IEnumerable<string> ChunkStringByTokens(string input, int maxTokens)
    {
        var tokens = tokenizer.Encode(input);

        for (int i = 0; i < tokens.Count; i += maxTokens)
        {
            var tokenChunk = tokens.Skip(i).Take(maxTokens).ToList();
            var chunk = tokenizer.Decode(tokenChunk);
            yield return chunk;
        }
    }

    const int maxEmbeddingTokens = 8192;

    foreach (var item in confluenceData)
    {
        if (!string.IsNullOrEmpty(item.Body))
        {
            var chunks = ChunkStringByTokens(item.Body, maxEmbeddingTokens); // Use tokenizer-based chunking!
            int chunkIndex = 1;

            foreach (var chunk in chunks)
            {
                var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(chunk);
                var doc = new
                {
                    id = Guid.NewGuid().ToString(),
                    content = chunk,
                    contentVector = embeddingResult
                        .Value.ToFloats()
                        .ToArray()
                        .Select(v => float.IsNaN(v) || float.IsInfinity(v) ? 0f : v)
                        .ToArray(), // not sure why we have to do this

                    originalUrl = item.Url,
                    chunkIndex = chunkIndex++,
                };
                docs.Add(doc);
            }
        }
    }

    // Batch upload to Azure AI Search
    await searchClient.UploadDocumentsAsync(docs);
}

Console.Write("Enter a search question: ");
var question = Console.ReadLine();

var questionEmbedding = (await embeddingClient.GenerateEmbeddingAsync(question)).Value.ToFloats();

var searchOptions = new SearchOptions
{
    VectorSearch = new VectorSearchOptions
    {
        Queries =
        {
            new VectorizedQuery(questionEmbedding)
            {
                KNearestNeighborsCount = 5,
                Fields = { "contentVector" },
            },
        },
    },

    Select = { "content", "originalUrl" },
};

var response = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);

var contextSb = new StringBuilder();
foreach (var result in response.Value.GetResults())
{
    var doc = result.Document;
    var content = doc.ContainsKey("content") ? doc["content"]?.ToString() : "";
    var originalUrl = doc.ContainsKey("originalUrl") ? doc["originalUrl"]?.ToString() : "";

    //Console.WriteLine($"URL: {originalUrl}");
    //Console.WriteLine("Content:");
    //Console.WriteLine(content);
    //Console.WriteLine("--------");

    contextSb.AppendLine(content);
}

// ... Your existing config

string prompt = $"""
You are an assistant. ONLY answer questions using the information below, which comes from an Azure AI Search.
If the answer is not present in the data, say "I don't know based on the provided information."

Context:
{contextSb}

Question: {question}
Answer:
""";

var chatClient = azureOpenAIClient.GetChatClient(chatDeploymentName);

var updates = chatClient.CompleteChatStreamingAsync(prompt);

Console.WriteLine();

await foreach (var update in updates)
{
    foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
    {
        Console.Write(updatePart.Text);
    }
}

Console.ReadLine();

async Task<List<ConfluencePageDto>> GetPublicConfluenceData()
{
    var rootUrl = $"https://{confluenceOrg}.atlassian.net/wiki";

    var spacesJsonText = await new HttpClient().GetStringAsync($"{rootUrl}/rest/api/space");
    var spacesJson = JsonSerializer.Deserialize<ConfluenceSpaceJson.Rootobject>(spacesJsonText);
    var pages = new List<ConfluencePageDto>();

    foreach (var space in spacesJson.results)
    {
        await GetPages(start: 0, space.name, space.key);
    }

    return pages;

    // nested function
    async Task GetPages(int start, string spaceName, string spaceKey)
    {
        var contentJsonText = await new HttpClient().GetStringAsync(
            $"{rootUrl}/rest/api/space/{spaceKey}/content/page?limit=100&start={start}&expand=body.storage"
        );
        var contentJson = JsonSerializer.Deserialize<ConfluencePageJson.Rootobject>(
            contentJsonText
        );

        foreach (var page in contentJson.results)
        {
            pages.Add(
                new ConfluencePageDto()
                {
                    Body = Utils.CleanHtml(page.body.storage.value),
                    Title = page.title,
                    Url = $"{rootUrl}{page._links.webui}",
                }
            );
        }

        if (contentJson._links.next != null)
        {
            await GetPages(start: start + 100, spaceName, spaceKey);
        }
    }
}