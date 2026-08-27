#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

#endregion

namespace PipelineInvokeTest.Pipelines
{
    public class PersonPipelineContext2 : IPipelineFlowContext<PersonDto>
    {

        public PipelineStepFailExecutionStrategyType FailExecutionStrategy
            => PipelineStepFailExecutionStrategyType.StepRetry;

        public bool IsEnabledStepResultCollector => true;
    }
}
