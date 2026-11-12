using System.Threading.Channels;

namespace Giraf_AI.Services.TTS;

/// <summary>
/// In-memory FIFO queue for synchronous TTS requests.
/// Limits all outstanding requests (waiting and processing) to 1,000.
/// Each HTTP request waits for its own WAV bytes; no audio or job data is persisted.
/// </summary>
public sealed class TtsQueueService
{
    private const int HardMaxOutstandingJobs = 1_000;

    private readonly Channel<TtsQueueItem> _channel;
    private readonly SemaphoreSlim _availableSlots;

    public int MaxOutstandingJobs { get; }

    public TtsQueueService(IConfiguration configuration)
    {
        int configuredLimit = configuration.GetValue<int?>("Tts:MaxOutstandingJobs")
                              ?? HardMaxOutstandingJobs;

        if (configuredLimit is < 1 or > HardMaxOutstandingJobs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(configuration),
                $"Tts:MaxOutstandingJobs must be between 1 and {HardMaxOutstandingJobs}.");
        }

        MaxOutstandingJobs = configuredLimit;
        _availableSlots = new SemaphoreSlim(configuredLimit, configuredLimit);
        _channel = Channel.CreateBounded<TtsQueueItem>(
            new BoundedChannelOptions(configuredLimit)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    internal bool TryEnqueue(
        string text,
        CancellationToken requestCancellationToken,
        out TtsQueueItem job)
    {
        job = new TtsQueueItem(Guid.NewGuid(), text, requestCancellationToken);

        // Count the job currently being synthesized as well as waiting jobs.
        // Do not block when full: the controller returns HTTP 503 immediately.
        if (!_availableSlots.Wait(0))
        {
            return false;
        }

        bool enqueued = false;
        try
        {
            enqueued = _channel.Writer.TryWrite(job);
            return enqueued;
        }
        finally
        {
            if (!enqueued)
            {
                _availableSlots.Release();
            }
        }
    }

    internal IAsyncEnumerable<TtsQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    internal void ReleaseSlot() => _availableSlots.Release();
}

internal sealed class TtsQueueItem
{
    internal TtsQueueItem(Guid id, string text, CancellationToken requestCancellationToken)
    {
        Id = id;
        Text = text;
        RequestCancellationToken = requestCancellationToken;
        Completion = new TaskCompletionSource<byte[]>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal Guid Id { get; }
    internal string Text { get; }
    internal CancellationToken RequestCancellationToken { get; }
    internal TaskCompletionSource<byte[]> Completion { get; }
}
