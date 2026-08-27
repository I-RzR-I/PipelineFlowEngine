#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class CancelingPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private readonly CancellationTokenSource _pipelineCancellationSource;
        private readonly int _retryIterations;
        private int _executionCount;

        public CancelingPipelineStep(CancellationTokenSource pipelineCancellationSource, int retryIterations = 3)
        {
            _pipelineCancellationSource = pipelineCancellationSource;
            _retryIterations = retryIterations;
        }

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public override int ExecutionOrderIndex => 10;

        public override bool IsEnabled => true;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = _retryIterations
        };

        public override Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            _pipelineCancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new PipeLineStepResult<PersonDto>());
        }
    }
}
