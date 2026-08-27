// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 20-07-2026 17:07
// 
//  Last Modified By : RzR
//  Last Modified On : 27-08-2026 12:13
//  ***********************************************************************
//  <copyright file="InternalCancellationExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Threading;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An internal cancellation extensions.
    /// </summary>
    /// =================================================================================================
    internal static class InternalCancellationExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An <see cref="OperationCanceledException" /> extension method that query if the current
        ///     exception represents a cancellation of the pipeline itself, and therefore must be re-
        ///     thrown instead of being converted into a failed step result.
        /// </summary>
        /// <param name="exception">The exception to act on. Intentionally unused.</param>
        /// <param name="pipelineToken">
        ///     The cancellation token supplied to the pipeline invocation.
        /// </param>
        /// <returns>
        ///     True if the pipeline itself was cancelled, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsPipelineCancellation(this OperationCanceledException exception,
            CancellationToken pipelineToken)
        {
            _ = exception;

            return pipelineToken.IsCancellationRequested;
        }
    }
}