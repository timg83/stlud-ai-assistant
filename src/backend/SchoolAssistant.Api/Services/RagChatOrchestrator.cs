using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SchoolAssistant.Api.Contracts;
using SchoolAssistant.Api.Options;

namespace SchoolAssistant.Api.Services;

public sealed class RagChatOrchestrator : IChatOrchestrator
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly SearchIndexService _searchIndexService;
    private readonly ILogger<RagChatOrchestrator> _logger;

    private const string SystemPromptNl = """
        Je bent een behulpzame AI-assistent voor een school. Je beantwoordt vragen van ouders en leerlingen
        op basis van het schoolbeleid en officiële schooldocumenten.

        REGELS:
        - Beantwoord ALLEEN vragen op basis van de aangeleverde bronnen hieronder.
        - Als de bronnen geen antwoord bevatten, zeg dat eerlijk en verwijs naar de schooladministratie.
        - Geef altijd aan uit welke bron je antwoord komt.
        - Antwoord in de taal van de vraag (Nederlands of Engels).
        - Wees vriendelijk, professioneel en beknopt.
        - Verzin GEEN informatie die niet in de bronnen staat.
        """;

    private const string SystemPromptEn = """
        You are a helpful AI assistant for a school. You answer questions from parents and students
        based on school policy and official school documents.

        RULES:
        - ONLY answer questions based on the provided sources below.
        - If the sources don't contain an answer, say so honestly and refer to the school administration.
        - Always indicate which source your answer comes from.
        - Answer in the language of the question (Dutch or English).
        - Be friendly, professional, and concise.
        - Do NOT make up information that is not in the sources.
        """;

    public RagChatOrchestrator(
        AzureOpenAIClient openAiClient,
        IOptions<AzureOpenAiOptions> openAiOptions,
        SearchIndexService searchIndexService,
        ILogger<RagChatOrchestrator> logger)
    {
        _chatClient = openAiClient.GetChatClient(openAiOptions.Value.ChatDeployment);
        _embeddingClient = openAiClient.GetEmbeddingClient(openAiOptions.Value.EmbeddingDeployment);
        _searchIndexService = searchIndexService;
        _logger = logger;
    }

    public async Task<ChatQueryResponse> QueryAsync(ChatQueryRequest request, CancellationToken cancellationToken)
    {
        var traceId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Processing query {TraceId}: {Question}", traceId, request.Question);

        // 1. Generate embedding for the user's question
        var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(request.Question, cancellationToken: cancellationToken);
        var queryVector = embeddingResult.Value.ToFloats();

        // 2. Hybrid search: keyword + vector (gracefully handle missing index)
        IReadOnlyList<Azure.Search.Documents.Models.SearchResult<Azure.Search.Documents.Models.SearchDocument>> searchResults;
        try
        {
            searchResults = await _searchIndexService.HybridSearchAsync(request.Question, queryVector, topK: 5, cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status is 404 or 403)
        {
            _logger.LogWarning(ex, "Search service error (HTTP {Status}). Returning no-sources response for query {TraceId}", ex.Status, traceId);
            var isEn = request.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) ?? false;
            var message = ex.Status == 403
                ? (isEn ? "The search service is not yet available. Please try again later."
                        : "De zoekdienst is nog niet beschikbaar. Probeer het later opnieuw.")
                : (isEn ? "No school documents have been indexed yet. Please upload documents first."
                        : "Er zijn nog geen schooldocumenten geïndexeerd. Upload eerst documenten.");
            return new ChatQueryResponse(
                message,
                "none",
                [],
                new ChatEscalation(
                    isEn ? "Please contact the school administration." : "Neem contact op met de schooladministratie.",
                    isEn ? "Contact page" : "Contactpagina", "/contact"),
                traceId);
        }

        // 3. Build context from search results
        var sources = new List<ChatSource>();
        var contextBuilder = new System.Text.StringBuilder();

        foreach (var result in searchResults)
        {
            var doc = result.Document;
            var sourceId = doc.GetString("sourceId");
            var title = doc.GetString("title");
            var chunkText = doc.GetString("chunkText");
            var chunkIndex = doc.TryGetValue("chunkIndex", out var ci) ? ci?.ToString() : "?";

            contextBuilder.AppendLine($"--- Bron: {title} (sectie {chunkIndex}) ---");
            contextBuilder.AppendLine(chunkText);
            contextBuilder.AppendLine();

            if (!sources.Any(s => s.SourceId == sourceId))
            {
                sources.Add(new ChatSource(sourceId, title, null, null));
            }
        }

        // 4. Determine system prompt based on locale
        var isEnglish = request.Question.Split(' ').Any(w => new[] { "the", "what", "when", "how", "is", "are", "do", "does" }.Contains(w, StringComparer.OrdinalIgnoreCase));
        var systemPrompt = isEnglish ? SystemPromptEn : SystemPromptNl;

        // 5. Build chat messages
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new SystemChatMessage($"BRONNEN:\n{contextBuilder}"),
            new UserChatMessage(request.Question)
        };

        // 6. Call Azure OpenAI
        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 1024
        };

        var chatResult = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
        var answerText = chatResult.Value.Content[0].Text;

        // 7. Determine confidence based on search results (Azure AI Search hybrid scores are typically 0.01-0.05)
        var confidence = searchResults.Count == 0 ? "none"
            : searchResults.Any(r => r.Score > 0.02) ? "high"
            : searchResults.Any(r => r.Score > 0.01) ? "medium"
            : "low";

        // 8. Build escalation if confidence is low
        ChatEscalation? escalation = confidence is "none" or "low"
            ? new ChatEscalation(
                isEnglish
                    ? "The answer could not be reliably found in the school documents. Please contact the school administration."
                    : "Het antwoord kon niet betrouwbaar gevonden worden in de schooldocumenten. Neem contact op met de schooladministratie.",
                isEnglish ? "Contact page" : "Contactpagina",
                "/contact")
            : null;

        _logger.LogInformation("Query {TraceId} answered with confidence {Confidence}, {SourceCount} sources", traceId, confidence, sources.Count);

        return new ChatQueryResponse(answerText, confidence, sources, escalation, traceId);
    }

    public async IAsyncEnumerable<StreamChatEvent> QueryStreamAsync(
        ChatQueryRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var traceId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Processing streaming query {TraceId}: {Question}", traceId, request.Question);

        // 1. Embed
        var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(request.Question, cancellationToken: cancellationToken);
        var queryVector = embeddingResult.Value.ToFloats();

        // 2. Search
        IReadOnlyList<Azure.Search.Documents.Models.SearchResult<Azure.Search.Documents.Models.SearchDocument>> searchResults;
        string? searchErrorMessage = null;
        StreamChatEvent? searchErrorDoneEvent = null;
        try
        {
            searchResults = await _searchIndexService.HybridSearchAsync(request.Question, queryVector, topK: 5, cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status is 404 or 403)
        {
            _logger.LogWarning(ex, "Search service error (HTTP {Status}) for streaming query {TraceId}", ex.Status, traceId);
            var isEn = request.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) ?? false;
            searchErrorMessage = ex.Status == 403
                ? (isEn ? "The search service is not yet available." : "De zoekdienst is nog niet beschikbaar.")
                : (isEn ? "No school documents indexed yet." : "Er zijn nog geen schooldocumenten geïndexeerd.");
            searchErrorDoneEvent = new StreamChatEvent("done", Confidence: "none", Sources: [],
                Escalation: new ChatEscalation(
                    isEn ? "Please contact the school administration." : "Neem contact op met de schooladministratie.",
                    isEn ? "Contact page" : "Contactpagina", "/contact"),
                TraceId: traceId);
            searchResults = Array.Empty<Azure.Search.Documents.Models.SearchResult<Azure.Search.Documents.Models.SearchDocument>>();
        }
        if (searchErrorMessage is not null)
        {
            yield return new StreamChatEvent("delta", Delta: searchErrorMessage);
            if (searchErrorDoneEvent is not null)
            {
                yield return searchErrorDoneEvent;
            }
            yield break;
        }

        // 3. Build context
        var sources = new List<ChatSource>();
        var contextBuilder = new System.Text.StringBuilder();
        foreach (var result in searchResults)
        {
            var doc = result.Document;
            var sourceId = doc.GetString("sourceId");
            var title = doc.GetString("title");
            var chunkText = doc.GetString("chunkText");
            var chunkIndex = doc.TryGetValue("chunkIndex", out var ci) ? ci?.ToString() : "?";
            contextBuilder.AppendLine($"--- Bron: {title} (sectie {chunkIndex}) ---");
            contextBuilder.AppendLine(chunkText);
            contextBuilder.AppendLine();
            if (!sources.Any(s => s.SourceId == sourceId))
                sources.Add(new ChatSource(sourceId, title, null, null));
        }

        var isEnglish = request.Question.Split(' ').Any(w => new[] { "the", "what", "when", "how", "is", "are", "do", "does" }.Contains(w, StringComparer.OrdinalIgnoreCase));
        var systemPrompt = isEnglish ? SystemPromptEn : SystemPromptNl;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new SystemChatMessage($"BRONNEN:\n{contextBuilder}"),
            new UserChatMessage(request.Question)
        };

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 1024
        };

        // 4. Stream chat completion
        var streamResult = _chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken);

        await foreach (var update in streamResult.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return new StreamChatEvent("delta", Delta: part.Text);
                }
            }
        }

        // 5. Send final metadata (Azure AI Search hybrid scores are typically 0.01-0.05)
        var confidence = searchResults.Count == 0 ? "none"
            : searchResults.Any(r => r.Score > 0.02) ? "high"
            : searchResults.Any(r => r.Score > 0.01) ? "medium"
            : "low";

        ChatEscalation? escalation = confidence is "none" or "low"
            ? new ChatEscalation(
                isEnglish ? "The answer could not be reliably found in the school documents. Please contact the school administration."
                          : "Het antwoord kon niet betrouwbaar gevonden worden in de schooldocumenten. Neem contact op met de schooladministratie.",
                isEnglish ? "Contact page" : "Contactpagina", "/contact")
            : null;

        yield return new StreamChatEvent("done", Confidence: confidence, Sources: sources, Escalation: escalation, TraceId: traceId);
    }
}
