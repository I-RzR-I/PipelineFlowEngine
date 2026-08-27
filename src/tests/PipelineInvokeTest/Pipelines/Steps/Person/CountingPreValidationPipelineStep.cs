#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class CountingPreValidationPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private readonly int _retryIterations;
        private int _executionCount;
        private int _preValidationCallCount;

        public CountingPreValidationPipelineStep(int retryIterations = 2)
            => _retryIterations = retryIterations;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public int PreValidationCallCount => Volatile.Read(ref _preValidationCallCount);

        public override int ExecutionOrderIndex => 30;

        public override bool IsEnabled => true;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = _retryIterations
        };

        public override Func<PersonDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => (_, _) =>
            {
                Interlocked.Increment(ref _preValidationCallCount);

                return Task.FromResult(true);
            };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Fail);
            result.SetMessage("CountingPreValidationPipelineStep — intentionally returns Fail.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
