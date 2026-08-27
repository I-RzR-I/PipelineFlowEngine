// ***********************************************************************
//  Assembly          : RzR.Shared.Services.PipelineFlowEngine
//  Author            : RzR
//  Created           : 2026-07-20 22:07
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-27 12:13
//  ***********************************************************************
//  <copyright file="IPipelineDispatchObserver.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.PipelineFlowEngine.Models;

#endregion

namespace RzR.PipelineFlowEngine.Abstractions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Receives the terminal outcome of pipeline steps dispatched fire-and-forget, whose result the
    ///     flow itself can no longer report.
    /// </summary>
    /// <remarks>
    ///     Implementation contract:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The implementation must be registered as a SINGLETON. The invoker captures the observer
    ///                 when it is constructed; a scoped observer would be resolved from the scope that resolved
    ///                 the invoker, and it is invoked only after the dispatched execution ends, which is after
    ///                 that scope was disposed. It would therefore be invoked through a disposed scope, or hold
    ///                 that scope and its connections alive for the whole background execution.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 It is invoked on a background thread, after <c>InvokeAsync</c> already returned to the
    ///                 caller. Nothing it does can influence the flow result the caller received.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 It must not throw. An exception is swallowed and logged by the engine, because it would
    ///                 otherwise reach the scheduler or the caller shutdown path.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 When scoped services are needed, the implementation should take an
    ///                 <c>IServiceScopeFactory</c> in its constructor and create its own scope per invocation.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <typeparam name="T">Generic type parameter of the pipeline item.</typeparam>
    /// =================================================================================================
    public interface IPipelineDispatchObserver<T> where T : class
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Called once a dispatched (fire-and-forget) pipeline step reached a terminal state.
        /// </summary>
        /// <param name="outcome">The outcome of the dispatched execution.</param>
        /// =================================================================================================
        void OnDispatchCompleted(PipelineDispatchOutcome outcome);
    }
}