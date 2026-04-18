using SchoolAssistant.Api.Contracts;

namespace SchoolAssistant.Api.Services;

public interface IChatOrchestrator
{
    Task<ChatQueryResponse> QueryAsync(ChatQueryRequest request, CancellationToken cancellationToken);
}

public interface IContentService
{
    Task<UploadContentResponse> UploadAsync(UploadContentRequest request, CancellationToken cancellationToken);
    Task<PublishContentResponse> PublishAsync(string sourceId, CancellationToken cancellationToken);
    Task<ReindexContentResponse> ReindexAsync(ReindexContentRequest request, CancellationToken cancellationToken);
}

public interface IReviewService
{
    Task<IReadOnlyList<ReviewItemResponse>> GetItemsAsync(CancellationToken cancellationToken);
}
