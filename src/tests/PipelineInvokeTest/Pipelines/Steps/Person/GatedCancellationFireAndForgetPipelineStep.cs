#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class GatedCancellationFireAndForgetPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private readonly TaskCompletionSource<bool> _entered =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _release =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _executionCount;

        public Task Entered => _entered.Task;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 51;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = 1,
            WaitSchedulerExecution = false,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
                SuccessInterval = TimeSpan.FromMilliseconds(20),
                FailInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public void Release() => _release.TrySetResult(true);

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            _entered.TrySetResult(true);

            await _release.Task.ConfigureAwait(false);

            throw new OperationCanceledException(
                "GatedCancellationFireAndForgetPipelineStep — abandoned because a shutdown was requested.");
        }
    }
}
