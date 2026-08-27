#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class UndefinedPreValidationFailStrategyPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string AppliedName = "UndefinedPreValidationFailStrategyPipelineStep Name";

        private int _executionCount;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 25;

        public override bool IsEnabled => true;

        public override PipelineStepPreValidationFailStrategyType PreValidationFailStrategy
            => PipelineStepPreValidationFailStrategyType.Undefined;

        public override Func<PersonDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => (_, _) => Task.FromResult(false);

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            pipelineStep.Name = AppliedName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage(
                "UndefinedPreValidationFailStrategyPipelineStep — body executed, which must not happen.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
