using System;
using Db = Acadify.Models.Db;

namespace Acadify.Models.AdminPages
{
    public class StudentAdvisor
    {
        public int StudentAdvisorId { get; set; }

        public int StudentId { get; set; }
        public Db.Student? Student { get; set; }

        public int AdvisorId { get; set; }
        public Db.Advisor? Advisor { get; set; }

        public DateTime ConnectedAt { get; set; } = DateTime.Now;
    }
}