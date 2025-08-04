// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-07-01 20:52
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-07-09 15:41
// ***********************************************************************
//  <copyright file="Service.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.Threading.Tasks;

#endregion

namespace PipelineInvokeTest.Services
{
    public class Service
    {
        public async Task<bool> ValidateInTrueDataAsync() => await Task.FromResult(true);
    }
}