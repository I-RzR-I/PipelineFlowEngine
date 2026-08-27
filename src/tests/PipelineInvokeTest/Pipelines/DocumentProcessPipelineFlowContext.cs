using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

namespace PipelineInvokeTest.Pipelines
{
    public class DocumentProcessPipelineFlowContext : IPipelineFlowContext<DocumentItemDto>
    {

        public PipelineStepFailExecutionStrategyType FailExecutionStrategy
            => PipelineStepFailExecutionStrategyType.StepRetry;

        public bool IsEnabledStepResultCollector => true;
    }
}
