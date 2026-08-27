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
using RzR.PipelineFlowEngine.Models.Result;
using RzR.PipelineFlowEngine.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineInvokeTest.Pipelines.Steps.Document
{
    public class DocSetOnApprovePipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocSetOnApprovePipelineStep> _logger;

        public DocSetOnApprovePipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetOnApprovePipelineStep>>();
        }

        public override int ExecutionOrderIndex => 3;

        public override bool IsEnabled => true;

        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        public override Func<DocumentItemDto, CancellationToken, Task<bool>> PreExecutionValidationAsync
            => async (currentObject, cancellationToken) =>
            {
                _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetOnApprovePipelineStep)}");

                var service = _serviceProvider.GetRequiredService<DocumentService>();

                var obj = await service.GetAsync(currentObject.Id, cancellationToken);
                if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse()
                    && obj.Status == DocStatusType.InProcess && obj.State == DocStateType.OnProcessing)
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

                pipelineStep.ModifiedAt = DateTime.Now;
                pipelineStep.ModifiedById = Guid.NewGuid();
                pipelineStep.State = DocStateType.OnProcessing;
                pipelineStep.Status = DocStatusType.OnApprove;

                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Set on approve.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<DocumentItemDto>.Failure()
                    .SetMessage("Failed set on approve")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<DocumentItemDto>();
            }
        }
    }
}
