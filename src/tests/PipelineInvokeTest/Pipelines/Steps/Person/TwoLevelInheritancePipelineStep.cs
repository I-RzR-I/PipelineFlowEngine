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

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class TwoLevelInheritancePipelineStep : IntermediatePersonPipelineStepBase
    {

        public const string AppliedName = "TwoLevelInheritanceApplied";

        public override int ExecutionOrderIndex => 1;

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Run);

            pipelineStep.Name = AppliedName;

            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("Two level inheritance step executed.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
