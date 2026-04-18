namespace SchoolAssistant.Api.Contracts;

public sealed record UploadContentRequest(
    string Title,
    string SourceType,
    string Language,
    string Owner,
    string? ExternalUrl);

public sealed record UploadContentResponse(string SourceId, string IngestStatus);

public sealed record PublishContentResponse(string SourceId, string PublishStatus, DateTimeOffset IndexedAt);

public sealed record ReindexContentRequest(string? SourceId, string? StatusFilter);

public sealed record ReindexContentResponse(string JobId, string Status);

public sealed record ReviewItemResponse(string ReviewId, string Category, string Status, string Summary);
