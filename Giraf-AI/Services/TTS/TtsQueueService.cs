using System.Collections.Concurrent;
using System.Threading.Channels;
using Giraf_AI.DTOs;

namespace Giraf_AI.Services.TTS;

/// <summary>
/// Bounded, in-memory FIFO queue for speech generation.
/// At most 1,000 jobs can be outstanding (waiting plus processing) at once.
/// A single background reader sends one request at a time to Plapre.
/// Job statuses are lost when the API process restarts.
/// </summary>
public sealed class TtsQueueService
{
    private const int HardMaxOutstandingJobs = 1_000;
    private static readonly TimeSpan StatusRetention = TimeSpan.FromHours(24);

    private readonly Channel<TtsQueueItem> _channel;
    private readonly SemaphoreSlim _availableSlots;
    private readonly ConcurrentDictionary<Guid, TtsJobStatus> _statuses = new();

    public int MaxOutstandingJobs { get; }

    public TtsQueueService(IConfiguration configuration)
    {
        // A lower limit can be used for local testing, but never above 1,000.
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

    internal bool TryEnqueue(string text, bool waitForCompletion, out TtsQueueItem job)
    {
        job = new TtsQueueItem(Guid.NewGuid(), text, waitForCompletion);

        // Atomically reserve a slot BEFORE publishing the job.
        // SemaphoreSlim.Wait(0) is nonblocking and safe under concurrent requests.
        // This counts the active Plapre job as well as jobs waiting in the channel.
        if (!_availableSlots.Wait(0))
        {
            return false;
        }

        bool enqueued = false;
        try
        {
            PruneOldStatuses();

            var status = new TtsJobStatus
            {
                Id = job.Id,
                Status = "queued",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            if (!_statuses.TryAdd(job.Id, status))
            {
                throw new InvalidOperationException("A TTS job ID collision occurred.");
            }

            // Never wait for capacity: reject immediately if the channel cannot accept it.
            enqueued = _channel.Writer.TryWrite(job);
            return enqueued;
        }
        finally
        {
            if (!enqueued)
            {
                _statuses.TryRemove(job.Id, out _);
                _availableSlots.Release();
            }
        }
    }

    public TtsJobStatus? GetStatus(Guid id) =>
        _statuses.TryGetValue(id, out TtsJobStatus? status) ? status : null;

    internal IAsyncEnumerable<TtsQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Called exactly once by the worker after a job succeeds, fails or is cancelled.
    /// </summary>
    internal void ReleaseSlot() => _availableSlots.Release();

    internal void MarkProcessing(Guid id) =>
        UpdateStatus(id, status => status with
        {
            Status = "processing",
            StartedAtUtc = DateTimeOffset.UtcNow
        });

    internal void MarkCompleted(Guid id, string audioUrl) =>
        UpdateStatus(id, status => status with
        {
            Status = "completed",
            AudioUrl = audioUrl,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

    internal void MarkFailed(Guid id, string message) =>
        UpdateStatus(id, status => status with
        {
            Status = "failed",
            Error = message,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

    private void UpdateStatus(Guid id, Func<TtsJobStatus, TtsJobStatus> update)
    {
        while (_statuses.TryGetValue(id, out TtsJobStatus? current))
        {
            if (_statuses.TryUpdate(id, update(current), current))
            {
                return;
            }
        }
    }

    private void PruneOldStatuses()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - StatusRetention;

        foreach (KeyValuePair<Guid, TtsJobStatus> entry in _statuses)
        {
            if (entry.Value.CompletedAtUtc is { } completedAt && completedAt < cutoff)
            {
                _statuses.TryRemove(entry.Key, out _);
            }
        }
    }
}

internal sealed class TtsQueueItem
{
    internal TtsQueueItem(Guid id, string text, bool waitForCompletion)
    {
        Id = id;
        Text = text;
        Completion = waitForCompletion
            ? new TaskCompletionSource<TtsResult>(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }

    internal Guid Id { get; }
    internal string Text { get; }
    internal TaskCompletionSource<TtsResult>? Completion { get; }
}
