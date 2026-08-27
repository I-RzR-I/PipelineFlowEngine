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

    public class ThrowsThenSucceedsScheduledPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string ThrownMessage =
            "ThrowsThenSucceedsScheduledPipelineStep — first iteration throws on purpose, a later one recovers.";

        public const string RecoveredName = "ThrowsThenSucceedsScheduledPipelineStep Recovered";

        private readonly bool _waitSchedulerExecution;
        private int _executionCount;

        public ThrowsThenSucceedsScheduledPipelineStep(bool waitSchedulerExecution = true)
            => _waitSchedulerExecution = waitSchedulerExecution;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 31;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = 2,
            WaitSchedulerExecution = _waitSchedulerExecution,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
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

            if (executionNumber == 1)
                throw new InvalidOperationException(ThrownMessage);

            pipelineStep.Name = RecoveredName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("ThrowsThenSucceedsScheduledPipelineStep — recovered on a later iteration.");

            return result.AsPipelineStepResult();
        }
    }
}
