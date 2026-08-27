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

    public class ThrowingFireAndForgetProbePipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string ThrownMessage =
            "ThrowingFireAndForgetProbePipelineStep — intentionally throws from a dispatched execution.";

        private int _executionCount;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 20;

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
                StopOnFailure = true,
                ThrowOnFailure = true,
                SuccessInterval = TimeSpan.FromMilliseconds(20),
                FailInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public override Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            throw new InvalidOperationException(ThrownMessage);
        }
    }
}
