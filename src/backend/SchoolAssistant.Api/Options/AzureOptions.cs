namespace SchoolAssistant.Api.Options;

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ChatDeployment { get; set; } = string.Empty;
    public string EmbeddingDeployment { get; set; } = string.Empty;
}

public sealed class AzureAiSearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
}

public sealed class CosmosDbOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public sealed class BlobStorageOptions
{
    public string ContainerName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class RateLimitingOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int QueueLimit { get; set; }
}

public sealed class WidgetSecurityOptions
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedLocales { get; set; } = [];
    public int MaxQuestionLength { get; set; } = 1000;
}
