#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class ThrowingCountingPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string ThrownMessage = "ThrowingCountingPipelineStep — intentionally throws every time.";

        private readonly int _retryIterations;
        private readonly bool _throwForeignCancellation;
        private readonly CancellationTokenSource _foreignCancellationSource;
        private int _executionCount;

        public ThrowingCountingPipelineStep(int retryIterations = 0, bool throwForeignCancellation = false)
        {
            _retryIterations = retryIterations;
            _throwForeignCancellation = throwForeignCancellation;

            if (throwForeignCancellation)
            {
                _foreignCancellationSource = new CancellationTokenSource();
                _foreignCancellationSource.Cancel();
            }
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

            if (_throwForeignCancellation)
                throw new TaskCanceledException(
                    "ThrowingCountingPipelineStep — foreign token cancellation.",
                    null,
                    _foreignCancellationSource.Token);

            throw new InvalidOperationException(ThrownMessage);
        }
    }
}
