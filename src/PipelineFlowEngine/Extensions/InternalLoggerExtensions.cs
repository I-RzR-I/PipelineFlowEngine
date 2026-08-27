// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 09:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
// ***********************************************************************
//  <copyright file="InternalLoggerExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.Logging;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Validation;
using System;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An internal logger extensions.
    /// </summary>
    /// =================================================================================================
    internal static class InternalLoggerExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An ILogger extension method that query if 'logger' is enabled log level.
        /// </summary>
        /// <param name="logger">The logger to act on.</param>
        /// <param name="level">The level.</param>
        /// <returns>
        ///     True if enabled log level, false if not.
        /// </returns>
        /// =================================================================================================
        internal static bool IsEnabledLogLevel(this ILogger logger, LogLevel level)
            => logger.IsNotNull() && logger.IsEnabled(level);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An ILogger extension method that if enabled write, tagging the entry with a stable event
        ///     identifier so sinks and alert rules can match on it instead of on the message text.
        /// </summary>
        /// <remarks>
        ///     The message is written as the log state itself instead of as a format template, so a message
        ///     that already contains braces is never reparsed as a structured template.
        /// </remarks>
        /// <param name="logger">The logger to act on.</param>
        /// <param name="level">The level.</param>
        /// <param name="eventId">The stable event identifier of the entry.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">(Optional) The exception.</param>
        /// =================================================================================================
        internal static void IfEnabledWrite(this ILogger logger, LogLevel level, EventId eventId, string message,
            Exception exception = null)
        {
            if (logger.IsEnabledLogLevel(level).IsFalse() || level.AreEquals(LogLevel.None)) return;

            message.ThrowIfArgNull(nameof(message));

            logger.Log(level, eventId, message, exception, (state, _) => state);
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An ILogger extension method that if enabled write.
        /// </summary>
        /// <param name="logger">The logger to act on.</param>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">(Optional) The exception.</param>
        /// =================================================================================================
        internal static void IfEnabledWrite(this ILogger logger, LogLevel level, string message, Exception exception = null)
        {
            if (logger.IsEnabledLogLevel(level).IsFalse()) return;

            switch (level)
            {
                case LogLevel.Trace:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogTrace(message);
                    else
                        logger.LogTrace(exception, message);
                    break;
                case LogLevel.Debug:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogDebug(message);
                    else
                        logger.LogDebug(exception, message);
                    break;
                case LogLevel.Information:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogInformation(message);
                    else
                        logger.LogInformation(exception, message);
                    break;
                case LogLevel.Warning:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogWarning(message);
                    else
                        logger.LogWarning(exception, message);
                    break;
                case LogLevel.Error:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogError(message);
                    else
                        logger.LogError(exception, message);
                    break;
                case LogLevel.Critical:
                    message.ThrowIfArgNull(nameof(message));

                    if (exception.IsNull())
                        logger.LogCritical(message);
                    else
                        logger.LogCritical(exception, message);
                    break;
                case LogLevel.None:
                default:
                    break;
            }
        }
    }
}