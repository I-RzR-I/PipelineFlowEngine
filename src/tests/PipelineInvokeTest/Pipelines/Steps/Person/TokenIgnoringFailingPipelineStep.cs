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

    public class TokenIgnoringFailingPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private readonly CancellationTokenSource _pipelineCancellationSource;
        private readonly int _retryIterations;
        private int _executionCount;

        public TokenIgnoringFailingPipelineStep(
            CancellationTokenSource pipelineCancellationSource,
            int retryIterations = 5)
        {
            _pipelineCancellationSource = pipelineCancellationSource;
            _retryIterations = retryIterations;
        }

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 70;

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
            if (executionNumber == 1)
                _pipelineCancellationSource.Cancel();

            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("TokenIgnoringFailingPipelineStep — intentionally returns Fail and ignores the token.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
