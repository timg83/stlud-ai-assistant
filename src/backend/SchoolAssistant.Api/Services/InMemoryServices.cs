using System.Collections.Concurrent;
using SchoolAssistant.Api.Contracts;

namespace SchoolAssistant.Api.Services;

public sealed class InMemoryChatOrchestrator : IChatOrchestrator
{
    public Task<ChatQueryResponse> QueryAsync(ChatQueryRequest request, CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "nl-NL" : request.Locale;
        var response = locale.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? new ChatQueryResponse(
                "This is a placeholder answer from the project skeleton. Retrieval and Azure integrations are not connected yet.",
                "low",
                new[] { new ChatSource("placeholder-source", "School Regulations", "section 1", null) },
                new ChatEscalation("Please contact the school administration for a verified answer.", "Contact page", "/contact"),
                Guid.NewGuid().ToString("N"))
            : new ChatQueryResponse(
                "Dit is een placeholder-antwoord uit de projectskeleton. Retrieval en Azure-integraties zijn nog niet gekoppeld.",
                "low",
                new[] { new ChatSource("placeholder-source", "Schoolreglement", "sectie 1", null) },
                new ChatEscalation("Neem contact op met de schooladministratie voor een geverifieerd antwoord.", "Contactpagina", "/contact"),
                Guid.NewGuid().ToString("N"));

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<StreamChatEvent> QueryStreamAsync(
        ChatQueryRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var result = await QueryAsync(request, cancellationToken);
        yield return new StreamChatEvent("delta", Delta: result.AnswerText);
        yield return new StreamChatEvent("done", Confidence: result.Confidence, Sources: result.Sources,
            Escalation: result.Escalation, TraceId: result.TraceId);
    }
}

public sealed class InMemoryContentService : IContentService
{
    private readonly ConcurrentDictionary<string, UploadContentRequest> _content = new();

    public Task<UploadContentResponse> UploadAsync(UploadContentRequest request, CancellationToken cancellationToken)
    {
        var sourceId = $"src-{Guid.NewGuid():N}";
        _content[sourceId] = request;
        return Task.FromResult(new UploadContentResponse(sourceId, "received"));
    }

    public Task<PublishContentResponse> PublishAsync(string sourceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PublishContentResponse(sourceId, "published", DateTimeOffset.UtcNow));
    }

    public Task<ReindexContentResponse> ReindexAsync(ReindexContentRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ReindexContentResponse($"job-{Guid.NewGuid():N}", "queued"));
    }
}

public sealed class InMemoryReviewService : IReviewService
{
    public Task<IReadOnlyList<ReviewItemResponse>> GetItemsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReviewItemResponse> items = new[]
        {
            new ReviewItemResponse("review-1", "unanswered", "open", "Vraag zonder voldoende brondekking")
        };

        return Task.FromResult(items);
    }
}
