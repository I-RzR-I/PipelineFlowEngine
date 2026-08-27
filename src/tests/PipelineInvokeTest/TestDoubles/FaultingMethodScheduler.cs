#region U S A G E S

using RzR.Scheduling.RecurringJobs.Abstractions;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class FaultingMethodScheduler : IMethodScheduler
    {
        private int _scheduleCallCount;

        public FaultingMethodScheduler(Exception toThrow = null)
            => ToThrow = toThrow ?? new InvalidOperationException(DefaultThrownMessage);

        public const string DefaultThrownMessage =
            "FaultingMethodScheduler — the scheduling infrastructure is unavailable.";

        public Exception ToThrow { get; set; }

        public int ScheduleCallCount => Volatile.Read(ref _scheduleCallCount);

        public IScheduledJob Schedule(ScheduledJobOptions options, Func<CancellationToken, Task> work)
        {
            Interlocked.Increment(ref _scheduleCallCount);

            throw ToThrow;
        }

        public IScheduledJob Schedule(ScheduledJobOptions options, params Func<CancellationToken, Task>[] work)
        {
            Interlocked.Increment(ref _scheduleCallCount);

            throw ToThrow;
        }

        public Task<bool> TryStopAsync(string jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task StopAllAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask DisposeAsync() => default;
    }
}
