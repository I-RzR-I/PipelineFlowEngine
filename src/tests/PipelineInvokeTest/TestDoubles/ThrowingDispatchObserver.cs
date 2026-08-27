#region U S A G E S

using PipelineInvokeTest.Models;
using RzR.PipelineFlowEngine.Abstractions;
using RzR.PipelineFlowEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class ThrowingDispatchObserver : IPipelineDispatchObserver<PersonDto>
    {

        public const string ThrownMessage =
            "ThrowingDispatchObserver — intentionally throws while receiving a dispatch outcome.";

        private readonly object _syncRoot = new object();
        private readonly List<PipelineDispatchOutcome> _outcomes = new List<PipelineDispatchOutcome>();

        private readonly TaskCompletionSource<PipelineDispatchOutcome> _firstInvocationSignal =
            new TaskCompletionSource<PipelineDispatchOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<PipelineDispatchOutcome> Invoked => _firstInvocationSignal.Task;

        public int OutcomeCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _outcomes.Count;
                }
            }
        }

        public PipelineDispatchOutcome[] Snapshot()
        {
            lock (_syncRoot)
            {
                return _outcomes.ToArray();
            }
        }

        public void OnDispatchCompleted(PipelineDispatchOutcome outcome)
        {
            lock (_syncRoot)
            {
                _outcomes.Add(outcome);
            }

            _firstInvocationSignal.TrySetResult(outcome);

            throw new InvalidOperationException(ThrownMessage);
        }
    }
}
