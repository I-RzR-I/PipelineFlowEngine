#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineInvokeTest.Models;
using PipelineInvokeTest.Pipelines;
using PipelineInvokeTest.Pipelines.Steps.Document;
using PipelineInvokeTest.Services;
using RzR.PipelineFlowEngine.Enums;
using RzR.PipelineFlowEngine.ServiceDependencyInjectionExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Tests
{
    [TestClass]
    public class DocumentProcessPipelineFlowTests
    {
        private IServiceCollection _serviceCollection;

        [TestInitialize]
        public void Init()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            _serviceCollection = serviceCollection;
        }

        [TestMethod]
        public async Task Document_Process_Test()
        {
            _serviceCollection.AddScoped<DocumentService>();

            _serviceCollection.RegisterPipelineFlowEngine<DocumentItemDto, DocumentProcessPipelineFlowContext>();

            _serviceCollection.AddPipelineFlowEngineSteps<DocumentItemDto>(
                new List<Type>
                {
                    typeof(DocSetCreatedPipelineStep),
                    typeof(DocSetInProcessPipelineStep),
                    typeof(DocSetOnApprovePipelineStep),
                    typeof(DocSetApprovedPipelineStep),
                    typeof(DocSetFinishedPipelineStep)
                });

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var invoker = localServiceProvider.GetPipelineFlowEngineInvoker<DocumentItemDto>();
            var service = localServiceProvider.GetRequiredService<DocumentService>();

            var obj = new DocumentItemDto
            {
                Id = Guid.NewGuid(),
                IsActive = true
            };
            await service.AddAsync(obj);

            var result = await invoker.InvokeAsync(obj);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.FlowResponse);
            Assert.AreEqual(PipelineStateType.Finish, result.State);
            Assert.AreEqual(PipelineStatusType.Success, result.Status);
        }

        [TestMethod]
        public async Task DocumentLookup_WithALiveToken_ReturnsTheStoredDocument()
        {

            _serviceCollection.AddScoped<DocumentService>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var service = localServiceProvider.GetRequiredService<DocumentService>();

            var document = new DocumentItemDto { Id = Guid.NewGuid(), IsActive = true };
            await service.AddAsync(document);

            using var cancellationTokenSource = new CancellationTokenSource();

            var found = await service.GetAsync(document.Id, cancellationTokenSource.Token);

            Assert.IsNotNull(found,
                "A lookup handed a live token must answer exactly like one made without a token; accepting a "
                + "token may not change what the lookup returns.");
            Assert.AreEqual(document.Id, found.Id,
                "The lookup must return the document that was asked for.");
        }

        [TestMethod]
        public async Task DocumentLookup_WithACancelledToken_ThrowsInsteadOfReturningTheDocument()
        {

            _serviceCollection.AddScoped<DocumentService>();

            var localServiceProvider = _serviceCollection.BuildServiceProvider();
            var service = localServiceProvider.GetRequiredService<DocumentService>();

            var document = new DocumentItemDto { Id = Guid.NewGuid(), IsActive = true };
            await service.AddAsync(document);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => service.GetAsync(document.Id, cancellationTokenSource.Token),
                "A lookup that takes a token must honour it. One that accepts a token and ignores it is worse "
                + "than one that never took it: a precondition forwarding the pipeline token would look "
                + "cancellable while still doing its work after the caller walked away.");
        }
    }
}
