using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelineInvokeTest.Enums;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Services;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Reflection.TypeParam;
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

namespace PipelineInvokeTest.Pipelines.Steps.Document
{
    public class DocSetFinishedPipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocSetFinishedPipelineStep> _logger;

        public DocSetFinishedPipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetFinishedPipelineStep>>();
        }

        public override int ExecutionOrderIndex => 5;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Schedule;

        public override PipelineFlowRetryPolicy RetrySchedulePolicy { get; protected set; }
            = new PipelineFlowRetryPolicy()
            {
                WaitSchedulerExecution = true,
                RetryIterations = 3,

                ExecutionSchedulerSettings = new ScheduledJobOptions()
                {
                    StopOnFailure = true,
                    ThrowOnFailure = false,
                    FailInterval = TimeSpan.FromMilliseconds(40),
                    SuccessInterval = TimeSpan.FromMilliseconds(20)
                }
            };

        public override Func<DocumentItemDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => async (currentObject, cancellationToken) =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetFinishedPipelineStep)}");

                var service = _serviceProvider.GetRequiredService<DocumentService>();

                var obj = await service.GetAsync(currentObject.Id, cancellationToken);
                if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse()
                    && obj.Status == DocStatusType.Approved && obj.State == DocStateType.OnProcessing)
                    return await Task.FromResult(true);
                else
                    return await Task.FromResult(false);
            };

        public override async Task<PipeLineStepResult<DocumentItemDto>> ExecuteStepAsync(
            DocumentItemDto pipelineStep,
            IPipelineFlowContext<DocumentItemDto> context,
            ILogger<PipelineFlowInvoker<DocumentItemDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<DocumentItemDto>();
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetState(PipelineStateType.Run);
                pipelineStep = pipelineStep.IfIsNull(new DocumentItemDto());

                if (PreExecutionValidationAsync.IsNotNull())
                {
                    var res = await PreExecutionValidationAsync.Invoke(pipelineStep, cancellationToken);
                    if (res.IsFalse())
                    {
                        result.SetResult(pipelineStep, PipelineStatusType.Fail);
                        result.SetState(PipelineStateType.Finish);
                        result.SetMessage($"Pre-validation failed on step {nameof(DocSetFinishedPipelineStep)}.");

                        return await Task.FromResult(result.AsPipelineStepResult());
                    }
                }

                pipelineStep.ModifiedAt = DateTime.Now;
                pipelineStep.ModifiedById = Guid.NewGuid();
                pipelineStep.State = DocStateType.Finished;
                pipelineStep.Status = DocStatusType.Approved;

                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Set finished.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<DocumentItemDto>.Failure()
                    .SetMessage("Failed set finished")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<DocumentItemDto>();
            }
        }
    }
}
