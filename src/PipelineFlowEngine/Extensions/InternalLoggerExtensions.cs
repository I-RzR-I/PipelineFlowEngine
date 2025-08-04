// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 09:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:39
// ***********************************************************************
//  <copyright file="InternalLoggerExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using Microsoft.Extensions.Logging;
using System;

#endregion

namespace PipelineFlowEngine.Extensions
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

                    logger.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    message.ThrowIfArgNull(nameof(message));

                    logger.LogDebug(message);
                    break;
                case LogLevel.Information:
                    message.ThrowIfArgNull(nameof(message));

                    logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    message.ThrowIfArgNull(nameof(message));

                    logger.LogWarning(message);
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