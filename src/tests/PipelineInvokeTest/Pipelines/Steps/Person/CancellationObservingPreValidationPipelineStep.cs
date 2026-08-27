#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class CancellationObservingPreValidationPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string AppliedName = "CancellationObservingPreValidationPipelineStep Name";

        private readonly TaskCompletionSource<bool> _preValidationEntered =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _executionCount;
        private CancellationToken _observedToken;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public Task PreValidationEntered => _preValidationEntered.Task;

        public CancellationToken ObservedToken => _observedToken;

        public override int ExecutionOrderIndex => 60;

        public override bool IsEnabled => true;

        public override Func<PersonDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => async (_, cancellationToken) =>
            {
                _observedToken = cancellationToken;
                _preValidationEntered.TrySetResult(true);

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {

                }

                cancellationToken.ThrowIfCancellationRequested();

                return true;
            };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            pipelineStep.Name = AppliedName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage(
                "CancellationObservingPreValidationPipelineStep — body executed.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
