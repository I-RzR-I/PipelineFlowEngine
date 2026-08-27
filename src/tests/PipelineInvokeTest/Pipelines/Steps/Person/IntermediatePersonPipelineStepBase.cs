#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Pipeline;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public abstract class IntermediatePersonPipelineStepBase : PipeLineFlowStep<PersonDto>
    {

        public override bool IsEnabled => true;
    }
}
