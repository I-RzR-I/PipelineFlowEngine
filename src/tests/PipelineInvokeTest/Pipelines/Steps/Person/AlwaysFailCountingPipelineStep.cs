#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class AlwaysFailCountingPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private readonly int _retryIterations;
        private readonly int _failExecutions;
        private int _executionCount;

        public AlwaysFailCountingPipelineStep(int retryIterations = 0, int failExecutions = -1)
        {
            _retryIterations = retryIterations;
            _failExecutions = failExecutions;
        }

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 10;

        public override bool IsEnabled => true;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = _retryIterations
        };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var executionNumber = Interlocked.Increment(ref _executionCount);
            var mustFail = _failExecutions < 0 || executionNumber <= _failExecutions;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);

            if (mustFail == false)
            {
                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetStatus(PipelineStatusType.Success);
                result.SetMessage("AlwaysFailCountingPipelineStep — configured failures were exhausted.");

                return await Task.FromResult(result.AsPipelineStepResult());
            }

            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("AlwaysFailCountingPipelineStep — intentionally returns Fail.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
