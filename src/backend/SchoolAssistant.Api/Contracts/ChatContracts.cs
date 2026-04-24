namespace SchoolAssistant.Api.Contracts;

public sealed record ChatQueryRequest(string Question, string Locale, string? SessionId);

public sealed record ChatSource(string SourceId, string Title, string? Locator, string? Url);

public sealed record ChatEscalation(string Message, string? ContactLabel, string? ContactUrl);

public sealed record ChatQueryResponse(
    string AnswerText,
    string Confidence,
    IReadOnlyList<ChatSource> Sources,
    ChatEscalation? Escalation,
    string TraceId);

public sealed record StreamChatEvent(string Type, string? Delta = null, string? Confidence = null,
    IReadOnlyList<ChatSource>? Sources = null, ChatEscalation? Escalation = null, string? TraceId = null);
