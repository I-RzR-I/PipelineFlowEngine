// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:35
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:22
// ***********************************************************************
//  <copyright file="FlowResultEventHelpers.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.ArraysExtensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable CheckNamespace

#endregion

namespace PipelineFlowEngine.Models.Result
{
    /// -------------------------------------------------------------------------------------------------
    /// <content>
    ///     Encapsulates the result events of a flow.
    /// </content>
    /// <seealso cref="PipelineFlowEngine.Models.Result.FlowResult{T}"/>
    /// =================================================================================================
    public abstract partial class FlowResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets flow event.
        /// </summary>
        /// <param name="event">The event. <see cref="PipelineFlowEvent"/></param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetFlowEvent(PipelineFlowEvent @event)
        {
            Events = Events.Append(@event);

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets flow event.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetFlowEvent(IEnumerable<PipelineFlowEvent> events)
        {
            Events = Events.ToArray().AppendItem(events.ToArray());

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets flow event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="flowName">Name of the flow.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetFlowEvent(LogLevel level, string flowName, string message)
        {
            Events = Events.Append(new PipelineFlowEvent(level, flowName, message));

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets flow event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="flowName">Name of the flow.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetFlowEvent(LogLevel level, string flowName, string message, Exception exception)
        {
            Events = Events.Append(new PipelineFlowEvent(level, flowName, message, exception));

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets flow event.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="flowName">Name of the flow.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetFlowEvent(LogLevel level, string flowName, Exception exception)
        {
            Events = Events.Append(new PipelineFlowEvent(level, flowName, exception.Message, exception));

            return this;
        }
    }
}