// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:33
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 15:11
// ***********************************************************************
//  <copyright file="PipelineFlowEvent.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.Logging;
using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

#endregion

namespace RzR.PipelineFlowEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A pipeline flow event data model.
    /// </summary>
    /// =================================================================================================
    public class PipelineFlowEvent
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the event level.
        /// </summary>
        /// <value>
        ///     The event level.
        /// </value>
        /// =================================================================================================
        public LogLevel EventLevel { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        /// =================================================================================================
        public string Name { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        /// <value>
        ///     The message.
        /// </value>
        /// =================================================================================================
        public string Message { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the exception.
        /// </summary>
        /// <value>
        ///     The exception.
        /// </value>
        /// =================================================================================================
        public Exception Exception { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the event date.
        /// </summary>
        /// <value>
        ///     The event date. Always stored in UTC.
        /// </value>
        /// =================================================================================================
        public DateTime EventDate { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowEvent"/> class.
        /// </summary>
        /// =================================================================================================
        public PipelineFlowEvent() => EventDate = DateTime.UtcNow;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowEvent"/> class.
        /// </summary>
        /// <param name="eventLevel">The event level.</param>
        /// <param name="name">The name.</param>
        /// <param name="message">The message.</param>
        /// =================================================================================================
        public PipelineFlowEvent(LogLevel eventLevel, string name, string message)
        {
            EventLevel = eventLevel;
            Name = name;
            Message = message;
            Exception = null;
            EventDate = DateTime.UtcNow;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="PipelineFlowEvent"/> class.
        /// </summary>
        /// <param name="eventLevel">The event level.</param>
        /// <param name="name">The name.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// =================================================================================================
        public PipelineFlowEvent(LogLevel eventLevel, string name, string message, Exception exception)
        {
            EventLevel = eventLevel;
            Name = name;
            Message = message;
            Exception = exception;
            EventDate = DateTime.UtcNow;
        }
    }
}