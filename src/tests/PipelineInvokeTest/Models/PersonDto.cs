// ***********************************************************************
//  Assembly         : RzR.Shared.Services.PipelineInvokeTest
//  Author           : RzR
//  Created On       : 2025-06-24 17:20
// 
//  Last Modified By : RzR
//  Last Modified On : 2025-06-24 17:20
// ***********************************************************************
//  <copyright file="PersonDto.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using System;

namespace PipelineInvokeTest.Models
{
    public class PersonDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime BlockedOn { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsActive { get; set; }
    }
}