#region U S A G E S

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly object _syncRoot = new object();
        private readonly List<RecordedLogEntry> _entries = new List<RecordedLogEntry>();
        private readonly List<PendingWait> _pendingWaits = new List<PendingWait>();

        public RecordedLogEntry[] Snapshot()
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
            }
        }

        public Task<RecordedLogEntry> WaitForAsync(Func<RecordedLogEntry, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            lock (_syncRoot)
            {
                foreach (var recorded in _entries)
                {
                    if (predicate(recorded))
                        return Task.FromResult(recorded);
                }

                var pending = new PendingWait(predicate);
                _pendingWaits.Add(pending);

                return pending.Completion.Task;
            }
        }

        public Task<RecordedLogEntry> WaitForAsync(string messageFragment)
        {
            if (messageFragment == null)
                throw new ArgumentNullException(nameof(messageFragment));

            return WaitForAsync(x => x.Message != null && x.Message.Contains(messageFragment));
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private void Record(RecordedLogEntry entry)
        {
            List<PendingWait> satisfied = null;

            lock (_syncRoot)
            {
                _entries.Add(entry);

                for (var index = _pendingWaits.Count - 1; index >= 0; index--)
                {
                    if (_pendingWaits[index].Predicate(entry) == false)
                        continue;

                    (satisfied ??= new List<PendingWait>()).Add(_pendingWaits[index]);
                    _pendingWaits.RemoveAt(index);
                }
            }

            if (satisfied == null)
                return;

            foreach (var pending in satisfied)
                pending.Completion.TrySetResult(entry);
        }

        private sealed class PendingWait
        {
            internal PendingWait(Func<RecordedLogEntry, bool> predicate)
            {
                Predicate = predicate;
                Completion = new TaskCompletionSource<RecordedLogEntry>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            internal Func<RecordedLogEntry, bool> Predicate { get; }

            internal TaskCompletionSource<RecordedLogEntry> Completion { get; }
        }

        public sealed class RecordedLogEntry
        {

            public RecordedLogEntry(LogLevel level, EventId eventId, string category, string message,
                Exception exception)
            {
                Level = level;
                EventId = eventId;
                Category = category;
                Message = message;
                Exception = exception;
            }

            public LogLevel Level { get; }

            public EventId EventId { get; }

            public string Category { get; }

            public string Message { get; }

            public Exception Exception { get; }
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly RecordingLoggerProvider _owner;
            private readonly string _category;

            internal RecordingLogger(RecordingLoggerProvider owner, string category)
            {
                _owner = owner;
                _category = category;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var message = formatter == null
                    ? state?.ToString()
                    : formatter(state, exception);

                _owner.Record(new RecordedLogEntry(logLevel, eventId, _category, message, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new NullScope();

            private NullScope()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
