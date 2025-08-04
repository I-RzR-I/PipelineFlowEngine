// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:32
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-23 23:39
// ***********************************************************************
//  <copyright file="PipelineStepExecutionStrategyType.cs" company="RzR SOFT & TECH">
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
    ///     Values that represent pipeline step execution strategy types.
    /// </summary>
    /// =================================================================================================
    public enum PipelineStepExecutionStrategyType
    {
        /// <summary>
        ///     An enum constant representing the add in queue option.
        /// </summary>
        AddInQueue,

        /// <summary>
        ///     An enum constant representing the force execute option (execute only added steps).
        /// </summary>
        ForceExecute,

        /// <summary>
        ///     An enum constant representing the priority execute option (execute steps with priority tag, then all remaining).
        /// </summary>
        PriorityExecute
    }
}