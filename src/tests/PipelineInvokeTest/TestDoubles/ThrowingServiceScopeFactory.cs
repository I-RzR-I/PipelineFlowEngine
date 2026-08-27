#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

#endregion

namespace PipelineInvokeTest.TestDoubles
{

    public sealed class ThrowingServiceScopeFactory : IServiceScopeFactory
    {

        public const string ThrownMessage =
            "ThrowingServiceScopeFactory — no service for type 'IPersonRepository' has been registered.";

        private int _createScopeCallCount;

        public int CreateScopeCallCount => Volatile.Read(ref _createScopeCallCount);

        public IServiceScope CreateScope()
        {
            Interlocked.Increment(ref _createScopeCallCount);

            throw new InvalidOperationException(ThrownMessage);
        }
    }
}
