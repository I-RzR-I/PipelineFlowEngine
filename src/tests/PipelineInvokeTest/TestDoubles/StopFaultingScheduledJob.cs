#region U S A G E S

using RzR.Scheduling.RecurringJobs.Abstractions;
using RzR.Scheduling.RecurringJobs.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class StopFaultingScheduledJob : IScheduledJob
    {

        public const string ThrownMessage = "StopFaultingScheduledJob — the job could not be stopped.";

        private readonly TaskCompletionSource<bool> _completion =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _stopCallCount;

        public int StopCallCount => Volatile.Read(ref _stopCallCount);

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public JobState State => JobState.Running;

        public int IterationCount => 0;

        public DateTimeOffset? LastRunAt => null;

        public DateTimeOffset? NextRunAt => null;

        public DateTimeOffset? LastSuccessAt => null;

        public Exception LastError => null;

        public Task Completion => _completion.Task;

        public void Finish() => _completion.TrySetResult(true);

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCallCount);

            return Task.FromException(new InvalidOperationException(ThrownMessage));
        }
    }
}
