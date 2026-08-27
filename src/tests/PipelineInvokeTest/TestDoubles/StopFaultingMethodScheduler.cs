#region U S A G E S

using RzR.Scheduling.RecurringJobs.Abstractions;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class StopFaultingMethodScheduler : IMethodScheduler
    {

        public StopFaultingScheduledJob Job { get; } = new StopFaultingScheduledJob();

        public IScheduledJob Schedule(ScheduledJobOptions options, Func<CancellationToken, Task> work) => Job;

        public IScheduledJob Schedule(ScheduledJobOptions options, params Func<CancellationToken, Task>[] work)
            => Job;

        public Task<bool> TryStopAsync(string jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task StopAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => default;
    }
}
