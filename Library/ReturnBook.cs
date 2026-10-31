using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Library
{
    public partial class ReturnBook : Form
    {
        private int selectedIssueId = -1;

        public ReturnBook()
        {
            InitializeComponent();
            LoadIssuedBooks();
        }

        private void homeBtn_Click(object sender, EventArgs e)
        {
            MainForm main = new MainForm();
            main.Show();
            this.Hide();
        }

        private void addBookBtn_Click(object sender, EventArgs e)
        {
            AddBook form = new AddBook();
            form.Show();
            this.Hide();
        }

        private void issueBookBtn_Click(object sender, EventArgs e)
        {
            IssueBook issue = new IssueBook();
            issue.Show();
            this.Hide();
        }

        private void logoutBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Hide();
                LoginForm login = new LoginForm();
                login.FormClosed += (s, args) => this.Close(); // closes this form when login closes
                login.Show();

            }
        }

        private void btnSearchIssued_Click(object sender, EventArgs e)
        {
            string personID = txtSearchIssued.Text.Trim();

            if (string.IsNullOrEmpty(personID))
            {
                MessageBox.Show("Please enter a valid ID number to search.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                string query = "SELECT * FROM issued_books WHERE TRIM(student_id) = TRIM(@student_id)";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@student_id", personID);

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            selectedIssueId = Convert.ToInt32(reader["id"]);
                            txtBookTitle.Text = reader["book_title"].ToString();
                            txtPersonName.Text = reader["student_name"].ToString();
                            txtPhone.Text = reader["phone"].ToString();
                            dateIssue.Value = DateTime.Parse(reader["issue_date"].ToString());
                            dateReturn.Value = DateTime.Parse(reader["return_date"].ToString());

                            // Load book cover
                            int bookId = Convert.ToInt32(reader["book_id"]);
                            LoadBookCover(bookId);
                        }
                        else
                        {
                            MessageBox.Show("No active issued book found with this ID.");
                            ClearForm();
                        }
                    }
                }
            }

        }

        private void LoadBookCover(int bookId)
        {
            using (var conn = DB.GetConnection())
            {
                string query = "SELECT cover FROM books WHERE id = @id";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    var result = cmd.ExecuteScalar();

                    if (result != DBNull.Value && result != null)
                    {
                        byte[] imgBytes = (byte[])result;
                        using (MemoryStream ms = new MemoryStream(imgBytes))
                        {
                            pictureBoxCover.Image = Image.FromStream(ms);
                            pictureBoxCover.SizeMode = PictureBoxSizeMode.StretchImage;
                        }
                    }
                    else
                    {
                        pictureBoxCover.Image = null;
                    }
                }
            }
        }

        private void btnMarkAsReturned_Click(object sender, EventArgs e)
        {
            if (selectedIssueId == -1)
            {
                MessageBox.Show("Please search for an issued book by ID first.");
                return;
            }

            try
            {
                using (var conn = DB.GetConnection())
                {
                    string getBookIdQuery = "SELECT book_id FROM issued_books WHERE id = @id";
                    int bookId = -1;

                    using (SQLiteCommand cmd = new SQLiteCommand(getBookIdQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedIssueId);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            bookId = Convert.ToInt32(result);
                    }

                    if (bookId == -1)
                    {
                        MessageBox.Show("Book ID not found.");
                        return;
                    }

                    // 1. Delete the record
                    string deleteQuery = "DELETE FROM issued_books WHERE id = @id";
                    using (SQLiteCommand cmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedIssueId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Update book quantity
                    string updateBookQuery = "UPDATE books SET quantity = quantity + 1 WHERE id = @bookId";
                    using (SQLiteCommand cmd = new SQLiteCommand(updateBookQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Book returned successfully!");
                    ClearForm();
                    LoadIssuedBooks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error returning book: " + ex.Message);
            }
        }

        private void LoadIssuedBooks()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    string query = @"SELECT ib.id, b.title, ib.student_name, ib.student_id, ib.phone, ib.issue_date, ib.return_date 
                             FROM issued_books ib
                             JOIN books b ON ib.book_id = b.id
                             ORDER BY ib.id DESC";

                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridIssued.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading issued books: " + ex.Message);
            }
        }

        private void ClearForm()
        {
            txtPersonName.Clear();
            txtBookTitle.Clear();
            txtPhone.Clear();
            dateIssue.Value = DateTime.Today;
            dateReturn.Value = DateTime.Today;
            pictureBoxCover.Image = null;
            selectedIssueId = -1;
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadIssuedBooks(); // Refresh each time the form becomes visible
        }

    }
}
