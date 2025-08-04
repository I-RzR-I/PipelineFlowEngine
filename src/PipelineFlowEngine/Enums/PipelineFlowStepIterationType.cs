// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 18:16
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-24 18:16
// ***********************************************************************
//  <copyright file="PipelineFlowStepIterationType.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent pipeline flow step iteration types.
    /// </summary>
    /// =================================================================================================
    public enum PipelineFlowStepIterationType
    {
        /// <summary>
        ///     An enum constant representing the first execution option.
        /// </summary>
        FirstExecution,

        /// <summary>
        ///     An enum constant representing the retry execution option.
        /// </summary>
        RetryExecution
    }
}