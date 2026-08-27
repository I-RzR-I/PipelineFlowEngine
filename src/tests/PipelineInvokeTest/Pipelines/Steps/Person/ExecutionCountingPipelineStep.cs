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

    public class ExecutionCountingPipelineStep : PipeLineFlowStep<PersonDto>
    {

        private const int DefaultExecutionOrderIndex = 1;

        private readonly int _executionOrderIndex;

        private int _executionCount;

        public ExecutionCountingPipelineStep() : this(DefaultExecutionOrderIndex)
        {
        }

        public ExecutionCountingPipelineStep(int executionOrderIndex)
        {
            _executionOrderIndex = executionOrderIndex;
        }

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public Func<Task> WhileInFlightAsync { get; set; }

        public override int ExecutionOrderIndex => _executionOrderIndex;

        public override bool IsEnabled => true;

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            if (WhileInFlightAsync != null)
                await WhileInFlightAsync.Invoke().ConfigureAwait(false);

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("ExecutionCountingPipelineStep — executed.");

            return result.AsPipelineStepResult();
        }
    }
}
