using System.ClientModel;
using System.Text;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using SchoolAssistant.Api.Contracts;
using SchoolAssistant.Api.Options;

namespace SchoolAssistant.Api.Services;

public sealed class DocumentIngestionService : IContentService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly SearchIndexService _searchIndexService;
    private readonly ILogger<DocumentIngestionService> _logger;

    private const int ChunkSize = 800;
    private const int ChunkOverlap = 100;

    public DocumentIngestionService(
        BlobContainerClient blobContainerClient,
        AzureOpenAIClient openAiClient,
        IOptions<AzureOpenAiOptions> openAiOptions,
        SearchIndexService searchIndexService,
        ILogger<DocumentIngestionService> logger)
    {
        _blobContainerClient = blobContainerClient;
        _embeddingClient = openAiClient.GetEmbeddingClient(openAiOptions.Value.EmbeddingDeployment);
        _searchIndexService = searchIndexService;
        _logger = logger;
    }

    public async Task<UploadContentResponse> UploadAsync(UploadContentRequest request, CancellationToken cancellationToken)
    {
        var sourceId = $"src-{Guid.NewGuid():N}";
        return new UploadContentResponse(sourceId, "received");
    }

    public async Task<PublishContentResponse> PublishAsync(string sourceId, CancellationToken cancellationToken)
    {
        return new PublishContentResponse(sourceId, "published", DateTimeOffset.UtcNow);
    }

    public async Task<ReindexContentResponse> ReindexAsync(ReindexContentRequest request, CancellationToken cancellationToken)
    {
        return new ReindexContentResponse($"job-{Guid.NewGuid():N}", "queued");
    }

    public async Task IngestPdfFromBlobAsync(string blobName, string title, string language, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ingestion of blob {BlobName}", blobName);

        await _searchIndexService.EnsureIndexExistsAsync(cancellationToken);

        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var downloadResponse = await blobClient.DownloadContentAsync(cancellationToken);
        var pdfBytes = downloadResponse.Value.Content.ToArray();

        var text = ExtractTextFromPdf(pdfBytes);
        _logger.LogInformation("Extracted {Length} characters from {BlobName}", text.Length, blobName);

        var chunks = ChunkText(text);
        _logger.LogInformation("Created {ChunkCount} chunks from {BlobName}", chunks.Count, blobName);

        var sourceId = $"src-{Guid.NewGuid():N}";
        var searchDocuments = new List<SearchDocument>();

        // Generate embeddings in batches of 4 with retry for rate limiting
        const int batchSize = 4;
        for (int batchStart = 0; batchStart < chunks.Count; batchStart += batchSize)
        {
            if (batchStart > 0)
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            var batchEnd = Math.Min(batchStart + batchSize, chunks.Count);
            var batchTexts = chunks.Skip(batchStart).Take(batchEnd - batchStart).Select(c => c.Text).ToList();

            OpenAI.Embeddings.OpenAIEmbeddingCollection? embeddingResult = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    var result = await _embeddingClient.GenerateEmbeddingsAsync(batchTexts, cancellationToken: cancellationToken);
                    embeddingResult = result.Value;
                    break;
                }
                catch (ClientResultException ex) when (ex.Message.Contains("429"))
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 30);
                    _logger.LogWarning("Rate limited on embedding batch {Batch}, retrying in {Delay}s (attempt {Attempt})", batchStart / batchSize, delay.TotalSeconds, attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            if (embeddingResult == null)
                throw new InvalidOperationException("Failed to generate embeddings after retries");

            for (int i = 0; i < embeddingResult.Count; i++)
            {
                var chunkIndex = batchStart + i;
                var chunk = chunks[chunkIndex];
                var embedding = embeddingResult[i];

                var doc = new SearchDocument
                {
                    ["id"] = $"{sourceId}-chunk-{chunkIndex}",
                    ["sourceId"] = sourceId,
                    ["title"] = title,
                    ["chunkText"] = chunk.Text,
                    ["language"] = language,
                    ["chunkIndex"] = chunkIndex,
                    ["pageNumber"] = chunk.Page,
                    ["contentVector"] = embedding.ToFloats().ToArray()
                };

                searchDocuments.Add(doc);
            }
        }

        await _searchIndexService.UploadDocumentsAsync(searchDocuments, cancellationToken);
        _logger.LogInformation("Indexed {Count} chunks for {BlobName} with sourceId {SourceId}", searchDocuments.Count, blobName, sourceId);
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        using var document = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString().Trim();
    }

    private static List<TextChunk> ChunkText(string text)
    {
        var chunks = new List<TextChunk>();
        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ").Trim();

        var words = text.Split(' ');
        int wordIndex = 0;
        int chunkIndex = 0;

        while (wordIndex < words.Length)
        {
            var chunkWords = words.Skip(wordIndex).Take(ChunkSize).ToArray();
            var chunkText = string.Join(' ', chunkWords);

            chunks.Add(new TextChunk(chunkText, chunkIndex + 1));

            wordIndex += Math.Max(1, ChunkSize - ChunkOverlap);
            chunkIndex++;
        }

        return chunks;
    }

    private sealed record TextChunk(string Text, int Page);
}
