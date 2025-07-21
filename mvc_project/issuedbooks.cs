using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace mvc_project
{
    public class issuedbooks
    {
        public int TransactionId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string BookId { get; set; }
        public string BookName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
    }
}