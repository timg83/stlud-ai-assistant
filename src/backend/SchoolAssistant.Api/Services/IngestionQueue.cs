using System.Threading.Channels;

namespace SchoolAssistant.Api.Services;

public sealed record IngestionWorkItem(string BlobName, string Title, string Language);

public sealed class IngestionQueue
{
    private readonly Channel<IngestionWorkItem> _channel = Channel.CreateUnbounded<IngestionWorkItem>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<IngestionWorkItem> Writer => _channel.Writer;
    public ChannelReader<IngestionWorkItem> Reader => _channel.Reader;
}
