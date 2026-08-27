#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

#endregion

namespace PipelineInvokeTest.Pipelines
{
    public class PersonPipelineContext : IPipelineFlowContext<PersonDto>
    {

        public PipelineStepFailExecutionStrategyType FailExecutionStrategy { get; set; }
            = PipelineStepFailExecutionStrategyType.Undefined;

        public bool IsEnabledStepResultCollector => true;
    }
}
