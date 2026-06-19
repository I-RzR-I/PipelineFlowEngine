// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-11 22:57
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-11 22:57
// ***********************************************************************
//  <copyright file="DocSetCreatedPipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

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
    public class DocSetCreatedPipelineStep : PipeLineFlowStep<DocumentItemDto>
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<DocSetCreatedPipelineStep> _logger;

        public DocSetCreatedPipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<DocSetCreatedPipelineStep>>();
        }

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 1;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineExecutionCommandType ExecutionCommand => PipelineExecutionCommandType.Simple;

        /// <inheritdoc />
        public override Func<DocumentItemDto, Task<bool>> PreExecutionValidationAsync
        => async currentObject =>
        {
            _logger.LogInformation($"Do pre-execution validation on step {nameof(DocSetCreatedPipelineStep)}");

            var service = _serviceProvider.GetRequiredService<DocumentService>();

            var obj = await service.GetAsync(currentObject.Id);
            if (obj.IsNotNull() && obj.IsActive.IsTrue() && obj.Id.IsEmpty().IsFalse())
                return await Task.FromResult(true);
            else
                return await Task.FromResult(false);
        };

        /// <inheritdoc />
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

                pipelineStep.CreatedAt = DateTime.Now;
                pipelineStep.CreatedById = Guid.NewGuid();
                pipelineStep.State = DocStateType.Created;
                pipelineStep.Status = DocStatusType.New;
                
                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Set created date and user.");
                result.SetStatus(PipelineStatusType.Success);

                var service = _serviceProvider.GetRequiredService<DocumentService>();
                await service.EditAsync(pipelineStep);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<DocumentItemDto>.Failure()
                    .SetMessage("Failed set created date and user")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<DocumentItemDto>();
            }
        }
    }
}

