// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 27-08-2026 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 27-08-2026 13:20
//  ***********************************************************************
//  <copyright file="ScheduledExecutionState.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S A G E S

using System;

#endregion

namespace RzR.PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     The outcome of the LAST iteration of one scheduled step execution, written by the work
    ///     delegate and read once the job completed. This class cannot be inherited.
    /// </summary>
    /// =================================================================================================
    internal sealed class ScheduledExecutionState
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     True when the last iteration ran the step body through without throwing.
        /// </summary>
        /// =================================================================================================
        public bool Completed;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The exception thrown by the step body on the last iteration, or null when it did not throw.
        /// </summary>
        /// =================================================================================================
        public Exception LastError;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The exception raised while the scope, the step or the flow context of the last iteration was
        ///     being built, or null when that setup succeeded. Kept apart from <see cref="LastError"/> so a
        ///     dispatch that never reached its body stays distinguishable from one whose body failed.
        /// </summary>
        /// =================================================================================================
        public Exception SetupError;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Clears the state at the start of an iteration, so that it describes that iteration alone.
        /// </summary>
        /// =================================================================================================
        public void BeginIteration()
        {
            Completed = false;
            LastError = null;
            SetupError = null;
        }
    }
}