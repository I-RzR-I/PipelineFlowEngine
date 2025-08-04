// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:34
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:20
// ***********************************************************************
//  <copyright file="FlowResult.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using PipelineFlowEngine.Enums;
using System.Collections.Generic;

#endregion

namespace PipelineFlowEngine.Models.Result
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Base properties/methods of the pipeline/pipeline-step flow
    /// </summary>
    /// <content>
    ///     Encapsulates the result of a flow.
    /// </content>
    /// =================================================================================================
    public abstract partial class FlowResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the flow response.
        /// </summary>
        /// <value>
        ///     The flow response.
        /// </value>
        /// =================================================================================================
        public T FlowResponse { get; private set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets a value indicating whether this object is success.
        /// </summary>
        /// <value>
        ///     True if this object is success, false if not.
        /// </value>
        /// =================================================================================================
        public bool IsSuccess { get; private set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the execution flow message.
        /// </summary>
        /// <value>
        ///     The message.
        /// </value>
        /// =================================================================================================
        public string Message { get; private set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the execution flow status.
        /// </summary>
        /// <value>
        ///     The status.
        /// </value>
        /// =================================================================================================
        public PipelineStatusType Status { get; private set; } = PipelineStatusType.Undefined;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the execution flow state.
        /// </summary>
        /// <value>
        ///     The state.
        /// </value>
        /// =================================================================================================
        public PipelineStateType State { get; private set; } = PipelineStateType.Undefined;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the execution flow events.
        /// </summary>
        /// <value>
        ///     The events.
        /// </value>
        /// =================================================================================================
        public IEnumerable<PipelineFlowEvent> Events { get; private set; }
            = new List<PipelineFlowEvent>();
    }
}