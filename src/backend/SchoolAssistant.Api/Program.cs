using System.Threading.RateLimiting;
using SchoolAssistant.Api.Contracts;
using SchoolAssistant.Api.Options;
using SchoolAssistant.Api.Services;

var builder = WebApplication.CreateBuilder(args);

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
var allowedOrigins = widgetSecuritySection.GetSection("AllowedOrigins").Get<string[]>() ?? [];
var allowedLocales = widgetSecuritySection.GetSection("AllowedLocales").Get<string[]>() ?? ["nl-NL", "en-US", "en-GB"];
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

app.Run();
