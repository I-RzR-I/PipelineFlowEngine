// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-06-30 16:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 15:42
// ***********************************************************************
//  <copyright file="PeronSetInactivePipelineStep.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.CommonExtensions.TypeParam;
using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Abstractions;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Extensions;
using PipelineFlowEngine.Models.Result;
using PipelineFlowEngine.Pipeline;
using PipelineInvokeTest.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Pipelines.Steps.Person
{
    public class PeronSetInactivePipelineStep : PipeLineFlowStep<PersonDto>
    {
        /// <inheritdoc />
        public override int ExecutionOrderIndex => 1;

        /// <inheritdoc />
        public override bool IsEnabled => true;

        /// <inheritdoc />
        public override PipelineStateType State => PipelineStateType.Undefined;

        /// <inheritdoc />
        public override PipelineStatusType Status => PipelineStatusType.Undefined;

        /// <inheritdoc />
        public override async Task<PipeLineStepResult<PersonDto>> ExecuteStepAsync(
            PersonDto pipelineStep,
            IPipelineFlowContext<PersonDto> context,
            ILogger<PipelineFlowInvoker<PersonDto>> logger,
            CancellationToken cancellationToken = default)
        {
            var result = new PipeLineStepResult<PersonDto>();
            //var result = PipeLineStepResult<PersonDto>.Instance;
            result.SetState(PipelineStateType.Initialize);
            try
            {
                result.SetState(PipelineStateType.Run);
                pipelineStep = pipelineStep.IfIsNull(new PersonDto());

                pipelineStep.IsActive = false;
                result.SetResult(pipelineStep, PipelineStatusType.Success);
                result.SetState(PipelineStateType.Finish);
                result.SetMessage("Changed person active status from default value to false.");
                result.SetStatus(PipelineStatusType.Success);

                return await Task.FromResult(result.AsPipelineStepResult());
            }
            catch (Exception e)
            {
                return PipeLineStepResult<PersonDto>.Failure()
                    .SetMessage("Failed set person active status")
                    .SetFlowEvent(LogLevel.Error, e.Message, e)
                    .SetState(PipelineStateType.Finish)
                    .SetStatus(PipelineStatusType.Fail)
                    .AsPipelineStepResult();
            }
        }
    }
}