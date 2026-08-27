// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 2026-07-20 22:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="PipelineFlowEventIds.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.Logging;

#endregion

namespace RzR.PipelineFlowEngine.Helpers
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     The stable log event identifiers emitted by the pipeline flow engine.
    /// </summary>
    /// <remarks>
    ///     The numeric values are a published contract. Log sinks, alert rules and dashboards match on
    ///     them, so an existing identifier must never be renumbered or reused for another meaning; a new
    ///     situation always takes the next free number instead.
    ///     Allocated ranges:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>3000-3099: fire-and-forget dispatch path.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// =================================================================================================
    internal static class PipelineFlowEventIds
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A scheduled step was dispatched fire-and-forget; its outcome is not observed by the flow.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId ScheduledStepDispatched = new(3001, nameof(ScheduledStepDispatched));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A dispatched scheduled step faulted in the background, after the invocation returned.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId ScheduledStepDispatchFaulted = new(3002, nameof(ScheduledStepDispatchFaulted));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A scheduled step failed to stop after the pipeline cancellation was requested.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId ScheduledStepStopFaulted = new(3003, nameof(ScheduledStepStopFaulted));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A dispatched scheduled step could not be resolved from the newly created service scope, so
        ///     the originally supplied instance is reused.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId ScheduledStepNotResolvable = new(3004, nameof(ScheduledStepNotResolvable));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A registered dispatch completion observer threw; the exception was swallowed so it cannot
        ///     break the scheduler or the caller shutdown path.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId DispatchObserverFaulted = new(3005, nameof(DispatchObserverFaulted));

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A scheduled step was dispatched fire-and-forget while no service scope factory was supplied,
        ///     so it keeps running against the service scope that resolved the invoker.
        /// </summary>
        /// =================================================================================================
        internal static readonly EventId ScheduledStepDispatchedWithoutScope
            = new(3006, nameof(ScheduledStepDispatchedWithoutScope));
    }
}