using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using SchoolAssistant.Api.Options;

namespace SchoolAssistant.Api.Services;

public sealed class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private readonly int _vectorSearchDimensions;

    public SearchIndexService(SearchIndexClient indexClient, SearchClient searchClient, IOptions<AzureAiSearchOptions> searchOptions, IOptions<AzureOpenAiOptions> openAiOptions)
    {
        _indexClient = indexClient;
        _searchClient = searchClient;
        _indexName = searchOptions.Value.IndexName;
        _vectorSearchDimensions = openAiOptions.Value.EmbeddingDimensions;
    }

    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var definition = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SimpleField("sourceId", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("title") { AnalyzerName = LexicalAnalyzerName.Values.NlMicrosoft },
                new SearchableField("chunkText") { AnalyzerName = LexicalAnalyzerName.Values.NlMicrosoft },
                new SimpleField("language", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsSortable = true },
                new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true },
                new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _vectorSearchDimensions,
                    VectorSearchProfileName = "vector-profile"
                }
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile("vector-profile", "hnsw-config") },
                Algorithms = { new HnswAlgorithmConfiguration("hnsw-config") }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
    }

    public async Task UploadDocumentsAsync(IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default)
    {
        var batch = IndexDocumentsBatch.Upload(documents.ToList());
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }

    public async Task DeleteBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        var escapedSourceId = sourceId.Replace("'", "''");
        var options = new SearchOptions { Filter = $"sourceId eq '{escapedSourceId}'", Select = { "id" }, Size = 1000 };
        var response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        var ids = new List<string>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            ids.Add(result.Document.GetString("id"));
        }
        if (ids.Count > 0)
        {
            var batch = IndexDocumentsBatch.Delete("id", ids);
            await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        }
    }

    public async Task RecreateIndexAsync(CancellationToken cancellationToken = default)
    {
        try { await _indexClient.DeleteIndexAsync(_indexName, cancellationToken); } catch (RequestFailedException) { }
        await EnsureIndexExistsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult<SearchDocument>>> HybridSearchAsync(
        string query, ReadOnlyMemory<float> queryVector, int topK = 5, CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions
        {
            Size = topK,
            Select = { "id", "sourceId", "title", "chunkText", "language", "chunkIndex", "pageNumber" },
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "contentVector" }
                    }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
        var results = new List<SearchResult<SearchDocument>>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(result);
        }

        return results;
    }
}
