// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-24 20:36
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:39
// ***********************************************************************
//  <copyright file="InternalEnumExtensions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using RzR.Extensions.Domain.Primitives;
using System;

#endregion

namespace RzR.PipelineFlowEngine.Extensions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An internal enum extensions.
    /// </summary>
    /// =================================================================================================
    internal static class InternalEnumExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     A T extension method that if is true.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="sourceEnumValue">The sourceEnumValue to act on.</param>
        /// <param name="compareEnumValue">The compare enum value.</param>
        /// <param name="positiveCompareEnumValue">The positive compare enum value.</param>
        /// <returns>
        ///     A T.
        /// </returns>
        /// =================================================================================================
        internal static T IfIsTrue<T>(this T sourceEnumValue, T compareEnumValue, T positiveCompareEnumValue)
            where T : Enum
            => sourceEnumValue.AreEquals(compareEnumValue) ? positiveCompareEnumValue : compareEnumValue;
    }
}