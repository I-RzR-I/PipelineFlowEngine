// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:32
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-23 23:39
// ***********************************************************************
//  <copyright file="PipelineStateType.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace RzR.PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent pipeline state types.
    /// </summary>
    /// =================================================================================================
    public enum PipelineStateType
    {
        /// <summary>
        ///     An enum constant representing the undefined option.
        /// </summary>
        Undefined,

        /// <summary>
        ///     An enum constant representing the initialize option.
        /// </summary>
        Initialize,

        /// <summary>
        ///     An enum constant representing the run option.
        /// </summary>
        Run,

        /// <summary>
        ///     An enum constant representing the skip option.
        /// </summary>
        Skip,

        /// <summary>
        ///     An enum constant representing the finish option.
        /// </summary>
        Finish
    }
}