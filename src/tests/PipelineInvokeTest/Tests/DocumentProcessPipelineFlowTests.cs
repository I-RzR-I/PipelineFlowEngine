// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-09 18:15
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-12 00:01
// ***********************************************************************
//  <copyright file="DocumentProcessPipelineFlowTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

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
    }
}