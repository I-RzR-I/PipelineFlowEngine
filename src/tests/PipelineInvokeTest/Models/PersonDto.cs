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
