using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace mvc_project.Models
{
    public class IssuedBook
    {
        public int TransactionId { get; set; } // same as IssueId
        public string StudentId { get; set; }
        public string BookId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
    }

}