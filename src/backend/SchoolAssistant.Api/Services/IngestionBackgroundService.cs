namespace SchoolAssistant.Api.Services;

public sealed class IngestionBackgroundService : BackgroundService
{
    private readonly IngestionQueue _queue;
    private readonly DocumentIngestionService _ingestionService;
    private readonly ILogger<IngestionBackgroundService> _logger;

    public IngestionBackgroundService(
        IngestionQueue queue,
        DocumentIngestionService ingestionService,
        ILogger<IngestionBackgroundService> logger)
    {
        _queue = queue;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _ingestionService.IngestPdfFromBlobAsync(item.BlobName, item.Title, item.Language, stoppingToken);
                _logger.LogInformation("Ingestion completed for {BlobName}", item.BlobName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Ingestion cancelled for {BlobName} due to shutdown", item.BlobName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion failed for {BlobName}", item.BlobName);
            }
        }
    }
}
