#region U S A G E S

using RzR.Scheduling.RecurringJobs.Abstractions;
using RzR.Scheduling.RecurringJobs.Helpers;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class NonInvokingMethodScheduler : IMethodScheduler
    {
        private int _scheduleCallCount;

        public int ScheduleCallCount => Volatile.Read(ref _scheduleCallCount);

        public IScheduledJob Schedule(ScheduledJobOptions options, Func<CancellationToken, Task> work)
        {
            Interlocked.Increment(ref _scheduleCallCount);

            return ScheduleEmptyIteration();
        }

        public IScheduledJob Schedule(ScheduledJobOptions options, params Func<CancellationToken, Task>[] work)
        {
            Interlocked.Increment(ref _scheduleCallCount);

            return ScheduleEmptyIteration();
        }

        public Task<bool> TryStopAsync(string jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task StopAllAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask DisposeAsync() => default;

        private static IScheduledJob ScheduleEmptyIteration()
            => MethodSchedulerService.Default.Schedule(
                new ScheduledJobOptions
                {
                    SuccessInterval = TimeSpan.FromMilliseconds(1),
                    FailInterval = TimeSpan.FromMilliseconds(1),
                    MaxIterations = 1,
                    StopOnFirstSuccess = true,
                    StopOnFailure = false,
                    ThrowOnFailure = false
                },
                _ => Task.CompletedTask);
    }
}
