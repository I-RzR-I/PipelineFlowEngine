#region U S I N G

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
    public class ThrowingFireAndForgetPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string ThrownMessage =
            "ThrowingFireAndForgetPipelineStep — intentional throw to verify Fix #2 fault isolation.";

        public override int ExecutionOrderIndex => 7;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new()
        {
            WaitSchedulerExecution = false,
            RetryIterations = 1,
            StopExecutionIfSuccessful = false,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
                FailInterval = TimeSpan.FromMilliseconds(20),
                SuccessInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public override Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
                PersonDto pipelineStep,
                IPipelineFlowContext<PersonDto> context,
                ILogger<PipelineFlowInvoker<PersonDto>> logger,
                CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(ThrownMessage);
    }
}
