#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
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

    public class SucceedsThenThrowsScheduledWaitPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string ThrownMessage =
            "SucceedsThenThrowsScheduledWaitPipelineStep — intentionally throws after a successful iteration.";

        public const string AppliedName = "SucceedsThenThrowsScheduledWaitPipelineStep Name";

        private readonly bool _throwOnFailure;
        private int _executionCount;

        public SucceedsThenThrowsScheduledWaitPipelineStep(bool throwOnFailure = false)
            => _throwOnFailure = throwOnFailure;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 30;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = 2,
            WaitSchedulerExecution = true,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = _throwOnFailure,
                SuccessInterval = TimeSpan.FromMilliseconds(20),
                FailInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var executionNumber = Interlocked.Increment(ref _executionCount);

            await Task.Yield();

            if (executionNumber > 1)
                throw new InvalidOperationException(ThrownMessage);

            pipelineStep.Name = AppliedName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("SucceedsThenThrowsScheduledWaitPipelineStep — first iteration succeeded.");

            return result.AsPipelineStepResult();
        }
    }
}
