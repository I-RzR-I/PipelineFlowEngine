#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class ThrowsOnceThenSucceedsPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string RecoveredName = "Recovered Person Name";

        public const int RunawayExecutionCeiling = 50;

        private readonly int _retryIterations;
        private int _executionCount;

        public ThrowsOnceThenSucceedsPipelineStep(int retryIterations = 3)
            => _retryIterations = retryIterations;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 10;

        public override bool IsEnabled => true;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = _retryIterations
        };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var executionNumber = Interlocked.Increment(ref _executionCount);
            if (executionNumber > RunawayExecutionCeiling)
                throw new InvalidOperationException(
                    $"ThrowsOnceThenSucceedsPipelineStep — executed {executionNumber} times, past the "
                    + $"{RunawayExecutionCeiling} execution ceiling.");

            if (executionNumber == 1)
                throw new InvalidOperationException(
                    "ThrowsOnceThenSucceedsPipelineStep — first execution throws on purpose.");

            pipelineStep.Name = RecoveredName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("ThrowsOnceThenSucceedsPipelineStep — recovered on retry.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
