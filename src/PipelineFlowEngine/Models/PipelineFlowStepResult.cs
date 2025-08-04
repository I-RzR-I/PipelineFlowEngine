// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 18:15
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-30 22:35
// ***********************************************************************
//  <copyright file="PipelineFlowStepResult.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Models.Result;

#endregion

namespace PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Encapsulates the result of a pipeline flow step.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// =================================================================================================
    public class PipelineFlowStepResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the name of the step.
        /// </summary>
        /// <value>
        ///     The name of the step.
        /// </value>
        /// =================================================================================================
        public string StepName { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the step result.
        /// </summary>
        /// <value>
        ///     The step result.
        /// </value>
        /// =================================================================================================
        public PipeLineStepResult<T> StepResult { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the step iteration.
        /// </summary>
        /// <value>
        ///     The step iteration.
        /// </value>
        /// =================================================================================================
        public PipelineFlowStepIterationType StepIteration { get; set; }
    }
}