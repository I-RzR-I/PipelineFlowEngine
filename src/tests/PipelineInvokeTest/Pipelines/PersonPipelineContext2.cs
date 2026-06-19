// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-06-24 17:22
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 21:35
// ***********************************************************************
//  <copyright file="PersonPipelineContext2.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Enums;

#endregion

namespace PipelineInvokeTest.Pipelines
{
    public class PersonPipelineContext2 : IPipelineFlowContext<PersonDto>
    {
        /// <inheritdoc />
        public PipelineStepFailExecutionStrategyType FailExecutionStrategy 
            => PipelineStepFailExecutionStrategyType.StepRetry;

        /// <inheritdoc />
        public bool IsEnabledStepResultCollector => true;
    }
}