using iTextSharp.text.pdf;
using iTextSharp.text;
using mvc_project.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace mvc_project.Controllers
{
    public class DefaultController : Controller
    {

        private readonly string connStr = System.Configuration.ConfigurationManager
       .ConnectionStrings["DefaultConnection"].ConnectionString;

        // Dummy admin credentials – real project मध्ये database वापरा
        private const string AdminUsername = "admin";
        private const string AdminPassword = "admin123";

        // GET: Default
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult Dashboard()
        {
            if (Session["AdminName"] == null)
                return RedirectToAction("Login");

            return View();
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public JsonResult GetCounts()
        {
            int totalBooks = 0, totalStudents = 0;
            //totalIssued = 0;

            using (var con = new SqlConnection("Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False"))
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = con;
                con.Open();

                cmd.CommandText = "SELECT COUNT(*) FROM Books";
                totalBooks = (int)cmd.ExecuteScalar();

                cmd.CommandText = "SELECT COUNT(*) FROM Students";
                totalStudents = (int)cmd.ExecuteScalar();

                //cmd.CommandText = "SELECT COUNT(*) FROM IssuedBooks";
                //totalIssued = (int)cmd.ExecuteScalar();
            }

            return Json(new
            {
                TotalBooks = totalBooks,
                TotalStudents = totalStudents,
                //TotalIssued = totalIssued
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetPendingRequestCount()
        {
            int count = 0;
            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string sql = "SELECT COUNT(*) FROM RequestedBooks WHERE RequestAccept = 'Pending'";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                count = (int)cmd.ExecuteScalar();
            }
            return Json(count, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Login()
        {
            ViewBag.Username = "";
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // ✅ Replace with DB validation in real project
            if (username == "admin" && password == "admin")
            {
                Session["AdminName"] = username;
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password";
            return View(); // stay on login page
        }



        public ActionResult Addbook()
        {
            ViewBag.NewBookId = GenerateNextBookId();

            return View();
        }


        private int GenerateNextBookId()
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT ISNULL(MAX(CAST(BookId AS INT)), 0) + 1 FROM Books";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }





        [HttpPost]
        public ActionResult SaveBook(string BookId, string Title, string Author, string Publisher, string ISBN, int Quantity)
        {
            string sql = @"
            INSERT INTO Books (BookId, Title, Author, Publisher, ISBN, Quantity)
            VALUES (@BookId, @Title, @Author, @Publisher, @ISBN, @Quantity);
        ";

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@BookId", BookId);
                cmd.Parameters.AddWithValue("@Title", Title);
                cmd.Parameters.AddWithValue("@Author", Author);
                cmd.Parameters.AddWithValue("@Publisher", Publisher);
                cmd.Parameters.AddWithValue("@ISBN", ISBN);
                cmd.Parameters.AddWithValue("@Quantity", Quantity);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            ViewBag.Message = "✅ Book added successfully!";
            return View("Addbook");
        }


        [HttpGet]
        public ActionResult AddStudent()
        {
            ViewBag.NewStudentId = GenerateStudentId();

      

            return View();
        }

        private string GenerateStudentId()
        {
            string newId = "STU001";

            using (SqlConnection con = new SqlConnection("Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False"))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 StudentId FROM Students WHERE StudentId LIKE 'STU%' ORDER BY StudentId DESC", con))
            {
                con.Open();
                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    string lastId = result.ToString(); // e.g., STU007

                    if (lastId.Length >= 6 && int.TryParse(lastId.Substring(3), out int number))
                    {
                        newId = "STU" + (number + 1).ToString("D3");
                    }
                }
            }

            return newId;
        }


        [HttpPost]
        public ActionResult AddStudent(string FullName, string Email, string Phone, string Department)
        {
            const string sql = @"
            INSERT INTO Students (FullName, Email, Phone, Department)
            VALUES (@FullName, @Email, @Phone, @Department);
        ";

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@FullName", FullName);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Phone", Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", Department ?? (object)DBNull.Value);

                con.Open();
                int rows = cmd.ExecuteNonQuery();

                ViewBag.Message = rows > 0
                    ? "✅ Student added successfully!"
                    : "❗ Insert failed.";
            }

            return View();
        }
















        public ActionResult IssueBook()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetStudentDetails(int id)
        {
            string fullName = "", email = "";
            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("SELECT FullName, Email FROM Students WHERE StudentId=@StudentId", con))
            {
                cmd.Parameters.AddWithValue("@StudentId", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { fullName = r["FullName"].ToString(); email = r["Email"].ToString(); }
            }
            return Json(new { fullName, email }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public ActionResult GetBookDetails(int id)
        {
            string title = "", author = "";
            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("SELECT Title, Author FROM Books WHERE BookId=@BookId", con))
            {
                cmd.Parameters.AddWithValue("@BookId", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { title = r["Title"].ToString(); author = r["Author"].ToString(); }
            }
            return Json(new { title, author }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult IssueBookw()
        {
            string studentId = Request.Form["StudentId"];
            string bookId = Request.Form["BookId"];
            DateTime issueDate = Convert.ToDateTime(Request.Form["IssueDate"]);
            DateTime dueDate = Convert.ToDateTime(Request.Form["DueDate"]);

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(@"
        INSERT INTO IssuedBooks (StudentId, BookId, IssueDate, DueDate)
        VALUES (@sid, @bid, @i, @d);
        SELECT SCOPE_IDENTITY();", con))
            {
                cmd.Parameters.AddWithValue("@sid", studentId);
                cmd.Parameters.AddWithValue("@bid", bookId);
                cmd.Parameters.AddWithValue("@i", issueDate);
                cmd.Parameters.AddWithValue("@d", dueDate);

                con.Open();
                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                ViewBag.Message = $"Book issued successfully! Transaction ID: {newId}";
                return View("IssueBook");
            }
        }































        public ActionResult Returnbook()
        {
            return View();
        }


        [HttpPost]
        public ActionResult ReturnBook(int issueId, DateTime returnDate)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("UPDATE IssuedBooks SET Status = @Status WHERE IssueId = @IssueId", con);
                cmd.Parameters.AddWithValue("@Status", "Returned");
                cmd.Parameters.AddWithValue("@IssueId", issueId);

                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid Issue ID" });
                }
            }
        }















        public ActionResult Calculatefine()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Calculate(string transactionId, DateTime returnDate)
        {
            DateTime issueDate, dueDate;
            int daysOverdue = 0;
            decimal fineAmount = 0;

            string connStr = "Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT IssueDate, DueDate FROM IssuedBooks WHERE IssueId = @TransactionId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            issueDate = Convert.ToDateTime(reader["IssueDate"]);
                            dueDate = Convert.ToDateTime(reader["DueDate"]);

                            if (returnDate > dueDate)
                            {
                                daysOverdue = (returnDate - dueDate).Days;
                                fineAmount = daysOverdue * 20; // ₹5 per day
                            }

                            ViewBag.IssueDate = issueDate.ToString("dd-MM-yyyy");
                            ViewBag.DueDate = dueDate.ToString("dd-MM-yyyy");
                            ViewBag.ReturnDate = returnDate.ToString("dd-MM-yyyy");
                            ViewBag.DaysOverdue = daysOverdue;
                            ViewBag.FineAmount = fineAmount;
                        }
                        else
                        {
                            ViewBag.FineAmount = "Invalid Transaction ID";
                        }
                    }
                }
            }

            return View("CalculateFine");
        }








        public ActionResult IssuedBooks(string searchTerm)
        {
            List<IssuedBook> books = new List<IssuedBook>();
            string connectionString = "Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT IssueId AS IssueId, StudentId, BookId, IssueDate, DueDate, Status FROM IssuedBooks";

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query += " WHERE StudentId LIKE @search OR BookId LIKE @search";
                }

                SqlCommand cmd = new SqlCommand(query, con);
                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    books.Add(new IssuedBook
                    {
                        TransactionId = Convert.ToInt32(reader["IssueId"]),
                        StudentId = reader["StudentId"].ToString(),
                        BookId = reader["BookId"].ToString(),
                        IssueDate = Convert.ToDateTime(reader["IssueDate"]),
                        DueDate = Convert.ToDateTime(reader["DueDate"]),
                        Status = reader["Status"].ToString()
                    });
                }
            }

            return View(books);
        }

        public ActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Result(string SearchType, string BookQuery, string StudentQuery)
        {
            DataTable resultTable = new DataTable();
            string query = "";
            string connStr = "Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                if (SearchType == "book")
                {
                    query = "SELECT BookId, Title, Author, Publisher, ISBN FROM Books WHERE Title LIKE @Query OR ISBN LIKE @Query";
                }
                else if (SearchType == "student")
                {
                    query = "SELECT StudentId, FullName, Email, Phone,Department FROM Students WHERE FullName LIKE @Query OR StudentId LIKE @Query";
                }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Query", "%" + (SearchType == "book" ? BookQuery : StudentQuery) + "%");
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(resultTable);
                    }
                }
            }

            ViewBag.SearchType = SearchType;
            ViewBag.ResultTable = resultTable;

            return View("Search");
        }

        public JsonResult GetRecentBooks()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT TOP 5 BookId, Title, Author, Quantity FROM Books ORDER BY BookId DESC", con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                var books = new List<object>();

                while (reader.Read())
                {
                    books.Add(new
                    {
                        BookId = Convert.ToInt32(reader["BookId"]),
                        Title = reader["Title"].ToString(),
                        Author = reader["Author"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"])
                    });
                }

                return Json(books, JsonRequestBehavior.AllowGet);
            }
        }




        public JsonResult GetRecentStudents()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT TOP 5 StudentId, FullName, Email, Phone FROM Students ORDER BY StudentId DESC", con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                var students = new List<object>();

                while (reader.Read())
                {
                    students.Add(new
                    {
                        StudentId = reader["StudentId"].ToString(),
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString(),
                        Phone = reader["Phone"].ToString()
                    });
                }

                return Json(students, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Renewbook()
        {
            return View();
        }


        public JsonResult GetIssuedBookByTransactionId(int transactionId)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT StudentId, BookId, IssueDate, DueDate FROM IssuedBooks WHERE IssueId = @IssueId", con);
                cmd.Parameters.AddWithValue("@IssueId", transactionId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var data = new
                    {
                        StudentId = reader["StudentId"].ToString(),
                        BookId = reader["BookId"].ToString(),
                        IssueDate = Convert.ToDateTime(reader["IssueDate"]).ToString("yyyy-MM-dd"),
                        DueDate = Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd")
                    };
                    return Json(data, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public ActionResult RenewBook(int issueId, DateTime newDueDate)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE IssuedBooks SET DueDate = @NewDueDate WHERE IssueId = @IssueId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IssueId", issueId);
                    cmd.Parameters.AddWithValue("@NewDueDate", newDueDate);
                   

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        TempData["Message"] = "Due date updated successfully.";
                    }
                    else
                    {
                        TempData["Message"] = "Invalid Transaction ID. No data updated.";
                    }
                }
            }

            return RedirectToAction("RenewBook");
        }



        public ActionResult GetTransactionDetails(int issueId)
        {
            string connectionString = "Data Source=DESKTOP-4QKGPC4\\SQLEXPRESS;Initial Catalog=books;Integrated Security=True;Encrypt=False";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT StudentId, BookId, IssueDate, DueDate, Status FROM IssuedBooks WHERE IssueId = @IssueId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@IssueId", issueId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var result = new
                    {
                        success = true,
                        studentId = reader["StudentId"].ToString(),
                        bookId = reader["BookId"].ToString(),
                        issueDate = Convert.ToDateTime(reader["IssueDate"]).ToString("yyyy-MM-dd"),
                        dueDate = Convert.ToDateTime(reader["DueDate"]).ToString("yyyy-MM-dd"),
                        status = reader["Status"].ToString()
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }

            // Record not found
            return Json(new { success = false, message = "Record not found." }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Requestbook()
        {
            List<Dictionary<string, string>> requests = new List<Dictionary<string, string>>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT * FROM RequestedBooks ORDER BY RequestDateTime DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var row = new Dictionary<string, string>
            {
                { "Id", rdr["Id"].ToString() }, // 👈 ADD THIS LINE
                { "BookTitle", rdr["BookTitle"].ToString() },
                { "Author", rdr["Author"].ToString() },
                { "Publisher", rdr["Publisher"].ToString() },
                { "Reason", rdr["Reason"].ToString() },
                { "RequestDateTime", Convert.ToDateTime(rdr["RequestDateTime"]).ToString("yyyy-MM-dd HH:mm:ss") },
                { "RequestAccept", rdr["RequestAccept"].ToString() }
            };
                    requests.Add(row);
                }
                rdr.Close();
            }
            ViewBag.Requests = requests;
            return View();
        }


        [HttpPost]
public ActionResult Requestbook(string BookTitle, string Author, string Publisher, string Reason, string Email)
{
    string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    using (SqlConnection con = new SqlConnection(connectionString))
    {
        string query = @"
            INSERT INTO RequestedBooks 
            (BookTitle, Author, Publisher, Reason, Email, RequestDateTime)
            VALUES (@BookTitle, @Author, @Publisher, @Reason, @Email, GETDATE())";
        
        SqlCommand cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@BookTitle", BookTitle);
        cmd.Parameters.AddWithValue("@Author", Author);
        cmd.Parameters.AddWithValue("@Publisher", Publisher);
        cmd.Parameters.AddWithValue("@Reason", Reason);
        cmd.Parameters.AddWithValue("@Email", Email);

        con.Open();
        cmd.ExecuteNonQuery();
        con.Close();
    }

    ViewBag.Message = "📬 Your request has been submitted successfully!";
    return View();
}

        public ActionResult Requestaccept()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetRequestDetails(int id)
        {
            var result = new
            {
                bookTitle = "",
                author = "",
                publisher = "",
                requestDateTime = "",
                requestStatus = ""
            };

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var cmd = new SqlCommand(@"
        SELECT BookTitle, Author, Publisher, RequestDateTime, RequestAccept
        FROM RequestedBooks
        WHERE Id = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = new
                        {
                            bookTitle = reader["BookTitle"].ToString(),
                            author = reader["Author"].ToString(),
                            publisher = reader["Publisher"].ToString(),
                            requestDateTime = Convert.ToDateTime(reader["RequestDateTime"]).ToString("dd-MM-yyyy hh:mm tt"),
                            requestStatus = reader["RequestAccept"].ToString()
                        };
                    }
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetRequestEmail(int id)
        {
            string conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string email = "";

            using (var con = new SqlConnection(conn))
            using (var cmd = new SqlCommand("SELECT Email FROM RequestedBooks WHERE Id = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    email = result.ToString();
            }

            return Json(new { email }, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public ActionResult UpdateRequestStatus(int RequestId, string RequestAccept)
        {
            string conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string userEmail = "";

            using (var con = new SqlConnection(conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE RequestedBooks SET RequestAccept = @Status WHERE Id = @Id; " +
                    "SELECT Email FROM RequestedBooks WHERE Id = @Id", con))
                {
                    cmd.Parameters.AddWithValue("@Status", RequestAccept);
                    cmd.Parameters.AddWithValue("@Id", RequestId);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                        userEmail = reader["Email"].ToString();
                    reader.Close();
                }
            }

            if (!string.IsNullOrEmpty(userEmail))
                SendStatusEmail(userEmail, RequestAccept);

            TempData["Message"] = "Status updated and email sent.";
            return RedirectToAction("Requestaccept");
        }
        private void SendStatusEmail(string toEmail, string status)
        {
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress("yourlibrary@example.com", "Library Admin"),
                    Subject = "Your Book Request Status Updated",
                    Body = $"Hello,\n\nYour book request is now: **{status}**.\n\nRegards,\nLibrary Team",
                    IsBodyHtml = false
                };
                mail.To.Add(toEmail);

                using (var smtp = new SmtpClient())
                {
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                // Log: email failed, but status already updated
                System.Diagnostics.Debug.WriteLine("Email send failed: " + ex.Message);
            }
        }


        [HttpPost]
        public ActionResult SendWhatsAppQRCode(string mobile, string qrContent)
        {
            // 🔗 Upload QR code image to your server and get its public URL
            string qrImageUrl = "https://yourdomain.com/images/generated_qr.png"; // हे तुमच्या QR कोड फोटोचं public link असावं

            // 📝 Final message to be sent
            string waMessage = $"📚 Library Fine Details:\n\n{qrContent}\n\n🔗 View QR Code: {qrImageUrl}";

            // 🔗 WhatsApp API link
            string whatsappLink = $"https://wa.me/91{mobile}?text={Uri.EscapeDataString(waMessage)}";

            // 🔁 Redirect to WhatsApp
            return Redirect(whatsappLink);
        }

        [HttpPost]
        public ActionResult SendEmailWithPDF(
     string email,
     string transactionId,
     string issueDate,
     string dueDate,
     string returnDate,
     string daysOverdue,
     string fineAmount)
        {
            try
            {
                // 1️⃣ Generate PDF
                MemoryStream memoryStream = new MemoryStream();
                Document document = new Document(PageSize.A4, 50, 50, 80, 60); // margins: left, right, top, bottom
                PdfWriter.GetInstance(document, memoryStream).CloseStream = false;
                document.Open();

                // Fonts
                var titleFont = FontFactory.GetFont("Arial", 18, Font.BOLD, BaseColor.BLACK);
                var labelFont = FontFactory.GetFont("Arial", 12, Font.BOLD);
                var valueFont = FontFactory.GetFont("Arial", 12, Font.NORMAL);

                // Header
                Paragraph header = new Paragraph("📚 Library Fine Receipt", titleFont);
                header.Alignment = Element.ALIGN_CENTER;
                header.SpacingAfter = 20;
                document.Add(header);

                // Create table
                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 35f, 65f });
                table.SpacingBefore = 10f;
                table.SpacingAfter = 20f;

                void AddRow(string label, string value)
                {
                    PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
                    labelCell.Border = Rectangle.NO_BORDER;
                    labelCell.PaddingBottom = 8f;

                    PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
                    valueCell.Border = Rectangle.NO_BORDER;
                    valueCell.PaddingBottom = 8f;

                    table.AddCell(labelCell);
                    table.AddCell(valueCell);
                }

                AddRow("Transaction ID:", transactionId);
                AddRow("Issue Date:", issueDate);
                AddRow("Due Date:", dueDate);
                AddRow("Return Date:", returnDate);
                AddRow("Days Overdue:", daysOverdue);
                AddRow("Fine Amount:", "₹" + fineAmount);

                document.Add(table);

                // QR Code
                string qrImagePath = Server.MapPath("~/QRCodeImages/upi_qr_code.png");
                if (System.IO.File.Exists(qrImagePath))
                {
                    Paragraph qrTitle = new Paragraph("📱 Scan to Pay", labelFont);
                    qrTitle.Alignment = Element.ALIGN_CENTER;
                    qrTitle.SpacingBefore = 10;
                    qrTitle.SpacingAfter = 10;
                    document.Add(qrTitle);

                    iTextSharp.text.Image qrImage = iTextSharp.text.Image.GetInstance(qrImagePath);
                    qrImage.ScaleAbsolute(100f, 100f);
                    qrImage.Alignment = Element.ALIGN_CENTER;
                    document.Add(qrImage);
                }

                document.Close();

                memoryStream.Position = 0;
                byte[] pdfBytes = memoryStream.ToArray();

                // 2️⃣ Send Email
                MailMessage message = new MailMessage();
                message.To.Add(email);
                message.From = new MailAddress("nikhilkale1617@gmail.com");
                message.Subject = "📄 Library Fine Receipt";
                message.Body = "Dear User,\n\nPlease find attached the fine receipt.\n\nThank you.";
                message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "FineReceipt.pdf"));

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("nikhilkale1617@gmail.com", "mgna zbmq ogrh iclj"); // Replace with app password
                smtp.EnableSsl = true;

                smtp.Send(message);

                TempData["EmailSuccess"] = " Email sent successfully to " + email;
            }
            catch (Exception ex)
            {
                TempData["EmailSuccess"] = " Error sending email: " + ex.Message;
            }

            return RedirectToAction("CalculateFine"); // View with TempData message
        }

    }

}
