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

    public class ForeignCancellationPreValidationPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string AppliedName = "ForeignCancellationPreValidationPipelineStep Name";

        private int _executionCount;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 70;

        public override bool IsEnabled => true;

        public override Func<PersonDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => async (_, _) =>
            {
                using (var foreignSource = new CancellationTokenSource())
                {
                    foreignSource.Cancel();

                    await Task.Delay(Timeout.Infinite, foreignSource.Token).ConfigureAwait(false);
                }

                return true;
            };

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
                "ForeignCancellationPreValidationPipelineStep — body executed, which must not happen.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
