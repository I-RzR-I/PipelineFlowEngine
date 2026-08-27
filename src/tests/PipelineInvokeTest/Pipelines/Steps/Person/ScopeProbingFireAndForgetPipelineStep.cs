#region U S A G E S

using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.TestDoubles;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.Extensions;
using RzR.PipelineFlowEngine.Models;
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using RzR.Scheduling.RecurringJobs.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{

    public class ScopeProbingFireAndForgetPipelineStep : PipeLineFlowStep<PersonDto>
    {

        public const string AppliedName = "ScopeProbingFireAndForgetPipelineStep Name";

        private readonly DispatchProbeRecorder _recorder;
        private readonly ScopedDisposalProbe _scopedDependency;

        public ScopeProbingFireAndForgetPipelineStep(
            DispatchProbeRecorder recorder,
            ScopedDisposalProbe scopedDependency)
        {
            _recorder = recorder;
            _scopedDependency = scopedDependency;
        }

        public ScopedDisposalProbe ScopedDependency => _scopedDependency;

        public override int ExecutionOrderIndex => 120;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand
            => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy => new PipelineFlowRetryPolicy
        {
            RetryIterations = 1,
            WaitSchedulerExecution = false,
            StopExecutionIfSuccessful = true,
            ExecutionSchedulerSettings = new ScheduledJobOptions
            {
                StopOnFailure = false,
                ThrowOnFailure = false,
                SuccessInterval = TimeSpan.FromMilliseconds(20),
                FailInterval = TimeSpan.FromMilliseconds(20)
            }
        };

        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            _recorder.Record(this, _scopedDependency.IsDisposed);

            pipelineStep.Name = AppliedName;

            var result = new PipeLineStepResult<PersonDto>();
            result.SetResult(pipelineStep, PipelineStatusType.Success);
            result.SetState(PipelineStateType.Finish);
            result.SetStatus(PipelineStatusType.Success);
            result.SetMessage("ScopeProbingFireAndForgetPipelineStep — executed.");

            return await Task.FromResult(result.AsPipelineStepResult());
        }
    }
}
