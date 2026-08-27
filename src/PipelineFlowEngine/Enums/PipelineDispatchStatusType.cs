// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 2026-07-20 22:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="PipelineDispatchStatusType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace RzR.PipelineFlowEngine.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent the terminal status of a fire-and-forget dispatched pipeline step.
    /// </summary>
    /// =================================================================================================
    public enum PipelineDispatchStatusType
    {
        /// <summary>
        ///     An enum constant representing the undefined option; the default value, never reported by the
        ///     engine.
        /// </summary>
        Undefined,

        /// <summary>
        ///     An enum constant representing a dispatched execution whose LAST iteration ran the step body
        ///     through without throwing. It does NOT mean the step returned a successful
        ///     <c>PipeLineStepResult</c>: the body of a dispatched step is never evaluated by the pipeline,
        ///     so a step that returns a failure result and a step that returns a success result are both
        ///     reported here. It also does not mean every iteration succeeded — an earlier iteration the
        ///     scheduler absorbed a failure from is not represented.
        /// </summary>
        Completed,

        /// <summary>
        ///     An enum constant representing a dispatched execution whose LAST iteration ended with an
        ///     exception, whether the scheduled job surfaced it or absorbed it. The exception is always
        ///     carried on the outcome. This includes an <see cref="System.OperationCanceledException"/>
        ///     raised by a token OTHER than the one supplied to the invocation — an HTTP client timeout is
        ///     the usual case — because nothing was shut down there: the step simply failed.
        /// </summary>
        Faulted,

        /// <summary>
        ///     An enum constant representing a dispatched execution that was canceled before it ended,
        ///     either because the scheduled job itself was canceled or because its last iteration ended
        ///     with an <see cref="System.OperationCanceledException"/> WHILE the token supplied to the
        ///     invocation was cancelled. The decision is made on that token and never on the exception type
        ///     alone, so a foreign cancellation is reported as <see cref="Faulted"/> instead. No exception
        ///     is carried: a genuine cancellation is an expected shutdown, not a fault.
        /// </summary>
        Canceled,

        /// <summary>
        ///     An enum constant representing a dispatched execution whose scheduled job RAN TO COMPLETION
        ///     without ever invoking the step body, and without anything failing: the iteration budget was
        ///     spent without an invocation, or the job was stopped before it reached one. It means the work
        ///     was scheduled and then quietly never happened, so it is the one outcome that always calls for
        ///     the schedule to be re-driven.
        ///     Three neighbouring situations are deliberately NOT reported here. A step body that ran and
        ///     threw is <see cref="Faulted"/>. A failure raised BEFORE the body, while the scope, the step
        ///     or the flow context was being built, is also <see cref="Faulted"/> and carries that
        ///     exception, so a broken registration is never mistaken for a spent iteration budget. And a
        ///     schedule the scheduler REJECTS produces no outcome at all: the rejection throws out of the
        ///     scheduling call, no job is ever created, and the failure surfaces on the invocation result.
        /// </summary>
        NotObserved
    }
}