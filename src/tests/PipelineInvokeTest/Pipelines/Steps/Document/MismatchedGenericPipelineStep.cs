#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Document
{

    public class MismatchedGenericPipelineStep : PipeLineFlowStep<DocumentItemDto>
    {

        public override int ExecutionOrderIndex => 0;

        public override bool IsEnabled => true;

        public override async Task<PipeLineStepResult<DocumentItemDto>> ExecuteStepAsync(
            DocumentItemDto pipelineStep,
            IPipelineFlowContext<DocumentItemDto> context,
            ILogger<PipelineFlowInvoker<DocumentItemDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<DocumentItemDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
