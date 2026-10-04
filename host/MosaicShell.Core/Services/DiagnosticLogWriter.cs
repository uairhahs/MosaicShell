using System.Threading.Channels;

namespace MosaicShell.Core.Services
{
    /// <summary>Where formatted lines land. The file store is the only production implementation.</summary>
    internal interface IDiagnosticLogStore
    {
        void Append(string path, IReadOnlyList<string> lines, long maxFileBytes);
    }

    internal sealed class FileDiagnosticLogStore : IDiagnosticLogStore
    {
        public static readonly FileDiagnosticLogStore Instance = new();

        public void Append(string path, IReadOnlyList<string> lines, long maxFileBytes)
        {
            DiagnosticLog.AppendLines(path, lines, maxFileBytes);
        }
    }

    /// <summary>
    /// Non-blocking diagnostic log for one run. <see cref="TryWrite"/> only queues: formatting and
    /// file I/O happen on a background pump, so a log call on the UI thread never waits on disk.
    /// The queue is bounded; when it is full the oldest lines are dropped and the next batch
    /// records how many, so memory stays bounded and the log says when it lost lines.
    /// </summary>
    public sealed class DiagnosticLogWriter : IDisposable
    {
        public const int DefaultCapacity = 4096;

        /// <summary>Lines taken per pump pass, so one file open covers many lines.</summary>
        public const int MaxBatchLines = 512;

        /// <summary>How long <see cref="Dispose"/> waits for queued lines to reach disk.</summary>
        public static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(2);

        private readonly Channel<Entry> _queue;
        private readonly string _directory;
        private readonly long _maxFileBytes;
        private readonly IDiagnosticLogStore _store;
        private readonly TimeProvider _clock;
        private readonly Task _pump;
        private long _dropped;
        private long _droppedSinceReport;
        private volatile bool _completed;

        public DiagnosticLogWriter(string directory, DiagnosticLogLevel minimumLevel, string? runId = null)
            : this(directory, minimumLevel, runId, DiagnosticLog.MaxFileBytes, DefaultCapacity, FileDiagnosticLogStore.Instance)
        {
        }

        internal DiagnosticLogWriter(
            string directory,
            DiagnosticLogLevel minimumLevel,
            string? runId,
            long maxFileBytes,
            int capacity,
            IDiagnosticLogStore store,
            TimeProvider? clock = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFileBytes);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            _directory = Path.GetFullPath(directory);
            MinimumLevel = minimumLevel;
            RunId = string.IsNullOrWhiteSpace(runId) ? DiagnosticLogFormat.NewRunId() : runId;
            _maxFileBytes = maxFileBytes;
            _store = store;
            _clock = clock ?? TimeProvider.System;
            _queue = Channel.CreateBounded<Entry>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                },
                OnDropped);
            _pump = Task.Run(PumpAsync);
        }

        public DiagnosticLogLevel MinimumLevel { get; }

        public string RunId { get; }

        /// <summary>Lines dropped because the queue was full, over the writer's lifetime.</summary>
        public long DroppedCount => Interlocked.Read(ref _dropped);

        /// <summary>Lets callers skip building a message nobody will write.</summary>
        public bool IsEnabled(DiagnosticLogLevel level)
        {
            return !_completed && level >= MinimumLevel;
        }

        /// <summary>
        /// Queues one line for <paramref name="fileName"/> in the log directory. Never blocks and
        /// never throws. Returns false when the level is filtered, the writer is disposed, or the
        /// file name is not a plain <c>name.log</c> (see <see cref="DiagnosticLogFormat.IsSafeFileName"/>).
        /// </summary>
        public bool TryWrite(string fileName, DiagnosticLogLevel level, string? message)
        {
            if (!IsEnabled(level) || !DiagnosticLogFormat.IsSafeFileName(fileName))
            {
                return false;
            }

            // Cap before queueing so a huge message cannot pin memory while it waits.
            string text = message ?? string.Empty;
            if (text.Length > DiagnosticLog.MaxMessageChars + 1)
            {
                text = text[..(DiagnosticLog.MaxMessageChars + 1)];
            }

            return _queue.Writer.TryWrite(new Entry(fileName, level, _clock.GetLocalNow(), text));
        }

        public void Dispose()
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _ = _queue.Writer.TryComplete();
            try
            {
                _ = _pump.Wait(DrainTimeout);
            }
            catch
            {
                // The pump swallows its own failures; nothing useful to do at shutdown.
            }
        }

        private void OnDropped(Entry dropped)
        {
            _ = Interlocked.Increment(ref _dropped);
            _ = Interlocked.Increment(ref _droppedSinceReport);
        }

        private async Task PumpAsync()
        {
            ChannelReader<Entry> reader = _queue.Reader;
            List<Entry> batch = new(MaxBatchLines);
            try
            {
                while (await reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (batch.Count < MaxBatchLines && reader.TryRead(out Entry entry))
                    {
                        batch.Add(entry);
                    }

                    WriteBatch(batch);
                    batch.Clear();
                }
            }
            catch
            {
                // Diagnostics must never take the process down.
            }
        }

        private void WriteBatch(List<Entry> batch)
        {
            if (batch.Count == 0)
            {
                return;
            }

            Dictionary<string, List<string>> byFile = new(StringComparer.OrdinalIgnoreCase);
            long dropped = Interlocked.Exchange(ref _droppedSinceReport, 0);
            if (dropped > 0)
            {
                Entry first = batch[0];
                Add(byFile, first.FileName, DiagnosticLogFormat.Line(
                    _clock.GetLocalNow(),
                    RunId,
                    DiagnosticLogLevel.Warning,
                    $"{dropped} log lines dropped: queue full"));
            }

            foreach (Entry entry in batch)
            {
                Add(byFile, entry.FileName, DiagnosticLogFormat.Line(entry.At, RunId, entry.Level, entry.Message));
            }

            foreach ((string fileName, List<string> lines) in byFile)
            {
                try
                {
                    _store.Append(Path.Combine(_directory, fileName), lines, _maxFileBytes);
                }
                catch
                {
                    // One failed file (locked, disk full) must not stop the others or later batches.
                }
            }
        }

        private static void Add(Dictionary<string, List<string>> byFile, string fileName, string line)
        {
            if (!byFile.TryGetValue(fileName, out List<string>? lines))
            {
                lines = [];
                byFile[fileName] = lines;
            }

            lines.Add(line);
        }

        private readonly record struct Entry(string FileName, DiagnosticLogLevel Level, DateTimeOffset At, string Message);
    }
}
