// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 20-07-2026 22:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="PipelineDispatchOutcome.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.PipelineFlowEngine.Enums;
using System;

#endregion

namespace RzR.PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An immutable description of one completed fire-and-forget dispatched execution. This
    ///     class cannot be inherited.
    /// </summary>
    /// <remarks>
    ///     The step is identified by name only.
    /// </remarks>
    /// =================================================================================================
    public sealed class PipelineDispatchOutcome
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineDispatchOutcome" /> class.
        /// </summary>
        /// <param name="stepName">The runtime type name of the dispatched pipeline step.</param>
        /// <param name="status">The terminal status of the dispatched execution.</param>
        /// <param name="exception">
        ///     (Optional) The exception raised by the dispatched execution, or null when it did not
        ///     fault.
        /// </param>
        /// =================================================================================================
        public PipelineDispatchOutcome(string stepName, PipelineDispatchStatusType status,
            Exception exception = null)
        {
            StepName = stepName;
            Status = status;
            Exception = exception;
            CompletedOnUtc = DateTime.UtcNow;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the runtime type name of the dispatched pipeline step.
        /// </summary>
        /// <value>
        ///     The dispatched step name.
        /// </value>
        /// =================================================================================================
        public string StepName { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the terminal status of the dispatched execution.
        /// </summary>
        /// <value>
        ///     The dispatch status.
        /// </value>
        /// =================================================================================================
        public PipelineDispatchStatusType Status { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the exception raised by the dispatched execution.
        /// </summary>
        /// <value>
        ///     The <see cref="AggregateException"/> reported for the dispatched execution, or null when it
        ///     did not fault.
        /// </value>
        /// =================================================================================================
        public Exception Exception { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the moment the outcome was produced.
        /// </summary>
        /// <value>
        ///     The completion date. Always stored in UTC.
        /// </value>
        /// =================================================================================================
        public DateTime CompletedOnUtc { get; }
    }
}