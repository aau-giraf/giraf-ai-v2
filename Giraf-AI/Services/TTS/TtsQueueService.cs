using System.Collections.Concurrent;
using System.Threading.Channels;
using Giraf_AI.DTOs;

namespace Giraf_AI.Services.TTS;

/// <summary>
/// Bounded, in-memory FIFO queue for speech generation.
/// A single background reader ensures that Plapre receives one request at a time.
/// Jobs and their statuses are lost when the API process restarts.
/// </summary>
public sealed class TtsQueueService
{
    private const int MaxQueuedJobs = 100;
    private static readonly TimeSpan StatusRetention = TimeSpan.FromHours(24);

    private readonly Channel<TtsQueueItem> _channel =
        Channel.CreateBounded<TtsQueueItem>(new BoundedChannelOptions(MaxQueuedJobs)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private readonly ConcurrentDictionary<Guid, TtsJobStatus> _statuses = new();

    internal bool TryEnqueue(string text, bool waitForCompletion, out TtsQueueItem job)
    {
        PruneOldStatuses();

        job = new TtsQueueItem(Guid.NewGuid(), text, waitForCompletion);
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

        // Reject immediately when the queue is full instead of holding HTTP requests open.
        if (_channel.Writer.TryWrite(job))
        {
            return true;
        }

        _statuses.TryRemove(job.Id, out _);
        return false;
    }

    public TtsJobStatus? GetStatus(Guid id) =>
        _statuses.TryGetValue(id, out TtsJobStatus? status) ? status : null;

    internal IAsyncEnumerable<TtsQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

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
