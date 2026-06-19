// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-01 17:45
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 15:42
// ***********************************************************************
//  <copyright file="PersonSetBlockedPipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

#endregion

// ReSharper disable RedundantLambdaParameterType

namespace PipelineInvokeTest.Pipelines.Steps.Person
{
    public class PersonSetBlockedPipelineStep : PipeLineFlowStep<PersonDto>
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<PersonSetBlockedPipelineStep> _logger;

        public PersonSetBlockedPipelineStep(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<PersonSetBlockedPipelineStep>>();
        }

        /// <inheritdoc />
        public override int ExecutionOrderIndex => 2;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineStateType State => PipelineStateType.Undefined;

        /// <inheritdoc />
        public override PipelineStatusType Status => PipelineStatusType.Undefined;

        /// <inheritdoc />
        public override Func<PersonDto, Task<bool>> PreExecutionValidationAsync { get; protected set; }
            = async (PersonDto currentObject) =>
            {
                _logger.LogInformation("Do pre-execution validation");
                var service = _serviceProvider.GetRequiredService<Service>();
                var validation = await service.ValidateInTrueDataAsync();

                return await Task.FromResult(validation && currentObject.IsActive.IsFalse());
            };

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            //var result = PipeLineStepResult<PersonDto>.Instance;
            var result = new PipeLineStepResult<PersonDto>();
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetState(PipelineStateType.Run);
                pipelineStep = pipelineStep.IfIsNull(new PersonDto());

                pipelineStep.IsBlocked = true;
                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Changed person blocked status in 'true'.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<PersonDto>.Failure()
                    .SetMessage("Failed set person blocked status")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult<PersonDto>();
            }
        }
    }
}