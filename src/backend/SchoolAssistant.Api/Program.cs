using System.Threading.RateLimiting;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.RateLimiting;
using SchoolAssistant.Api.Contracts;
using SchoolAssistant.Api.Options;
using SchoolAssistant.Api.Services;

var builder = WebApplication.CreateBuilder(args);

static string[] ReadIndexedSectionValues(IConfigurationSection parentSection, string sectionName, string[] fallback)
{
    var values = parentSection
        .AsEnumerable()
        .Where(entry =>
            entry.Key.StartsWith($"{parentSection.Path}:{sectionName}:", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(entry.Value))
        .Select(entry => entry.Value!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return values.Length > 0 ? values : fallback;
}

builder.Services.Configure<AzureOpenAiOptions>(builder.Configuration.GetSection("AzureOpenAi"));
builder.Services.Configure<AzureAiSearchOptions>(builder.Configuration.GetSection("AzureAiSearch"));
builder.Services.Configure<CosmosDbOptions>(builder.Configuration.GetSection("CosmosDb"));
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<WidgetSecurityOptions>(builder.Configuration.GetSection("WidgetSecurity"));

var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitingSection.GetValue<int>("PermitLimit", 20);
var windowSeconds = rateLimitingSection.GetValue<int>("WindowSeconds", 60);
var queueLimit = rateLimitingSection.GetValue<int>("QueueLimit", 0);
var widgetSecuritySection = builder.Configuration.GetSection("WidgetSecurity");
var allowedOrigins = ReadIndexedSectionValues(widgetSecuritySection, "AllowedOrigins", []);
var allowedLocales = ReadIndexedSectionValues(widgetSecuritySection, "AllowedLocales", ["nl-NL", "en-US", "en-GB"]);
var maxQuestionLength = widgetSecuritySection.GetValue<int>("MaxQuestionLength", 1000);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("public-chat", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
        limiterOptions.QueueLimit = queueLimit;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("widget", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IChatOrchestrator, InMemoryChatOrchestrator>();
builder.Services.AddSingleton<IContentService, InMemoryContentService>();

// --- Azure SDK clients (used when endpoints are configured) ---
var azureOpenAiEndpoint = builder.Configuration["AzureOpenAi:Endpoint"];
var azureSearchEndpoint = builder.Configuration["AzureAiSearch:Endpoint"];
var azureSearchIndexName = builder.Configuration["AzureAiSearch:IndexName"] ?? "school-assistant-index";
var blobStorageConnectionString = builder.Configuration["BlobStorage:ConnectionString"];
var blobContainerName = builder.Configuration["BlobStorage:ContainerName"] ?? "source-documents";
var storageAccountName = builder.Configuration["BlobStorage:AccountName"];

var credential = new DefaultAzureCredential();

if (!string.IsNullOrWhiteSpace(azureOpenAiEndpoint) && !string.IsNullOrWhiteSpace(azureSearchEndpoint))
{
    // Azure OpenAI client
    builder.Services.AddSingleton(_ => new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), credential));

    // Azure AI Search clients
    builder.Services.AddSingleton(_ => new SearchIndexClient(new Uri(azureSearchEndpoint), credential));
    builder.Services.AddSingleton(sp =>
    {
        var indexClient = sp.GetRequiredService<SearchIndexClient>();
        return indexClient.GetSearchClient(azureSearchIndexName);
    });

    // Blob Storage client
    if (!string.IsNullOrWhiteSpace(blobStorageConnectionString))
    {
        builder.Services.AddSingleton(_ => new BlobServiceClient(blobStorageConnectionString).GetBlobContainerClient(blobContainerName));
    }
    else
    {
        var blobUri = !string.IsNullOrWhiteSpace(storageAccountName)
            ? new Uri($"https://{storageAccountName}.blob.core.windows.net")
            : null;

        if (blobUri != null)
        {
            builder.Services.AddSingleton(_ => new BlobServiceClient(blobUri, credential).GetBlobContainerClient(blobContainerName));
        }
        else
        {
            throw new InvalidOperationException(
                "Blob Storage is not configured. Set either the blob storage connection string or the storage account name so the application can create the blob container client at startup.");
        }
    }

    builder.Services.AddSingleton<SearchIndexService>();
    builder.Services.AddSingleton<DocumentIngestionService>();

    // Replace in-memory implementations with Azure-backed ones
    builder.Services.AddSingleton<IChatOrchestrator, RagChatOrchestrator>();
    builder.Services.AddSingleton<IContentService>(sp => sp.GetRequiredService<DocumentIngestionService>());
}

builder.Services.AddSingleton<IReviewService, InMemoryReviewService>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'self'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseCors("widget");
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/chat/query", async (ChatQueryRequest request, IChatOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = new[] { "Question is required." }
        });
    }

    if (request.Question.Length > maxQuestionLength)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = [$"Question may contain at most {maxQuestionLength} characters."]
        });
    }

    if (allowedLocales.Length > 0 && !allowedLocales.Contains(request.Locale, StringComparer.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["locale"] = new[] { "Locale is not supported." }
        });
    }

    var response = await orchestrator.QueryAsync(request, cancellationToken);
    return Results.Ok(response);
})
    .RequireRateLimiting("public-chat");

app.MapPost("/api/chat/stream", async (ChatQueryRequest request, IChatOrchestrator orchestrator, HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        httpContext.Response.StatusCode = 400;
        return;
    }

    if (request.Question.Length > maxQuestionLength)
    {
        httpContext.Response.StatusCode = 400;
        return;
    }

    if (allowedLocales.Length > 0 && !allowedLocales.Contains(request.Locale, StringComparer.OrdinalIgnoreCase))
    {
        httpContext.Response.StatusCode = 400;
        return;
    }

    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    var cancellationToken = httpContext.RequestAborted;

    await foreach (var evt in orchestrator.QueryStreamAsync(request, cancellationToken))
    {
        var json = System.Text.Json.JsonSerializer.Serialize(evt, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
})
    .RequireRateLimiting("public-chat");

app.MapPost("/api/content/upload", async (UploadContentRequest request, IContentService contentService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SourceType))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = new[] { "Title and SourceType are required." }
        });
    }

    if (request.Title.Length > 200)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["title"] = new[] { "Title may contain at most 200 characters." }
        });
    }

    var response = await contentService.UploadAsync(request, cancellationToken);
    return Results.Accepted($"/api/content/{response.SourceId}", response);
});

app.MapPost("/api/content/publish/{sourceId}", async (string sourceId, IContentService contentService, CancellationToken cancellationToken) =>
{
    var response = await contentService.PublishAsync(sourceId, cancellationToken);
    return Results.Ok(response);
});

app.MapPost("/api/content/reindex", async (ReindexContentRequest request, IContentService contentService, CancellationToken cancellationToken) =>
{
    var response = await contentService.ReindexAsync(request, cancellationToken);
    return Results.Accepted($"/api/jobs/{response.JobId}", response);
});

app.MapGet("/api/review/items", async (IReviewService reviewService, CancellationToken cancellationToken) =>
{
    var response = await reviewService.GetItemsAsync(cancellationToken);
    return Results.Ok(response);
});

app.MapPost("/api/content/ingest", async (IngestRequest request, DocumentIngestionService? ingestionService, CancellationToken cancellationToken) =>
{
    if (ingestionService is null)
    {
        return Results.Problem("Document ingestion is not configured. Azure endpoints are required.", statusCode: 503);
    }

    if (string.IsNullOrWhiteSpace(request.BlobName) || string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["BlobName and Title are required."]
        });
    }

    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Ingestion");
    var blobName = request.BlobName;
    var title = request.Title;
    var language = request.Language ?? "nl";

    _ = Task.Run(async () =>
    {
        try
        {
            await ingestionService.IngestPdfFromBlobAsync(blobName, title, language, CancellationToken.None);
            logger.LogInformation("Background ingestion completed for {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background ingestion failed for {BlobName}", blobName);
        }
    });

    return Results.Accepted(value: new { status = "ingestion-started", blobName = request.BlobName });
});

app.MapPost("/api/admin/ensure-index", async (SearchIndexService? searchIndexService, CancellationToken cancellationToken) =>
{
    if (searchIndexService is null)
    {
        return Results.Problem("Search index service is not configured.", statusCode: 503);
    }

    await searchIndexService.EnsureIndexExistsAsync(cancellationToken);
    return Results.Ok(new { status = "index-ready" });
});

app.MapPost("/api/admin/recreate-index", async (SearchIndexService? searchIndexService, CancellationToken cancellationToken) =>
{
    if (searchIndexService is null)
    {
        return Results.Problem("Search index service is not configured.", statusCode: 503);
    }

    await searchIndexService.RecreateIndexAsync(cancellationToken);
    return Results.Ok(new { status = "index-recreated" });
});

app.Run();
