#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class MutatingThenFailingPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public static string MutatedNameFor(int attemptNumber) => $"MutatedByAttempt-{attemptNumber}";

        public List<string> ObservedNamesOnEntry { get; } = new List<string>();

        public override int ExecutionOrderIndex => 0;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            WaitSchedulerExecution = true,
            RetryIterations = 2
        };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            ObservedNamesOnEntry.Add(pipelineStep.Name);

            pipelineStep.Name = MutatedNameFor(ObservedNamesOnEntry.Count);

            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("Mutated the pipeline item and then failed on purpose.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
