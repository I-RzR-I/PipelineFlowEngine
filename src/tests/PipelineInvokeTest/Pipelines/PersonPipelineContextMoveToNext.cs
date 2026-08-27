#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

#endregion

namespace PipelineInvokeTest.Pipelines
{

    public class PersonPipelineContextMoveToNext : IPipelineFlowContext<PersonDto>
    {

        public PipelineStepFailExecutionStrategyType FailExecutionStrategy
            => PipelineStepFailExecutionStrategyType.StepMoveToNext;

        public bool IsEnabledStepResultCollector => true;
    }
}
