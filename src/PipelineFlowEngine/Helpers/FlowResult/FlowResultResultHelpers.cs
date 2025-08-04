// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineFlowEngine
//  Author           : RzR
//  Created On       : 2025-06-23 23:35
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-25 14:28
// ***********************************************************************
//  <copyright file="FlowResultResultHelpers.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.DataTypeExtensions;
using Microsoft.Extensions.Logging;
using PipelineFlowEngine.Enums;
using PipelineFlowEngine.Extensions;
using PipelineFlowEngine.Helpers;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable ArrangeThisQualifier
// ReSharper disable CheckNamespace

#endregion

namespace PipelineFlowEngine.Models.Result
{
    /// -------------------------------------------------------------------------------------------------
    /// <content>
    ///     Encapsulates the result methods of a flow.
    /// </content>
    /// <seealso cref="PipelineFlowEngine.Models.Result.FlowResult{T}"/>
    /// =================================================================================================
    public abstract partial class FlowResult<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow success data.
        /// </summary>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        internal FlowResult<T> SetSuccess()
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetSuccess.FormatWith(this.GetType().GetFlowName()));

            Events = Events;
            Status = PipelineStatusType.Success;
            State = PipelineStateType.Finish;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow success data.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        internal FlowResult<T> SetSuccess(T response)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetSuccessWithResult.FormatWith(this.GetType().GetFlowName()));

            Events = Events;
            Status = PipelineStatusType.Success;
            State = PipelineStateType.Finish;

            IsSuccess = IsOkStatus();
            FlowResponse = response;

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow failure data.
        /// </summary>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        internal FlowResult<T> SetFailure()
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetFailure.FormatWith(this.GetType().GetFlowName()));

            Events = Events;
            Status = PipelineStatusType.Fail;
            State = PipelineStateType.Finish;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow failure data.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        internal FlowResult<T> SetFailure(string message)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetFailureWithMessage.FormatWith(this.GetType().GetFlowName()));

            Message = message;
            Events = Events;
            Status = PipelineStatusType.Fail;
            State = PipelineStateType.Finish;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow result data.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="status">(Optional) The status. Default value is <see cref="PipelineStatusType.Undefined"/></param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetResult(T response, PipelineStatusType status = PipelineStatusType.Undefined)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetResult.FormatWith(this.GetType().GetFlowName()));

            FlowResponse = response;
            Status = status.IfIsTrue(PipelineStatusType.Undefined, Status);
            State = PipelineStateType.Finish;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets a execution flow message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetMessage(string message)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SeMessage.FormatWith(this.GetType().GetFlowName(), Message, message));

            Message = message;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow status.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetStatus(PipelineStatusType status)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetStatus.FormatWith(this.GetType().GetFlowName(), Status, status));

            Status = status;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the flow state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>
        ///     A FlowResult&lt;T&gt;
        /// </returns>
        /// =================================================================================================
        public FlowResult<T> SetState(PipelineStateType state)
        {
            SetFlowEvent(LogLevel.Information, this.GetType().Name,
                DefaultMessagesHelper.FlowResultEventMessage.SetState.FormatWith(this.GetType().GetFlowName(), State, state));

            State = state;

            IsSuccess = IsOkStatus();

            return this;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Query if this object is ok status.
        /// </summary>
        /// <returns>
        ///     True if ok status, false if not.
        /// </returns>
        /// =================================================================================================
        private bool IsOkStatus()
            => Status == PipelineStatusType.Success
               && Events.Any(x =>
                   new List<LogLevel>
                   {
                       LogLevel.Critical,
                       LogLevel.Error
                   }.Contains(x.EventLevel)).IsFalse();
    }
}