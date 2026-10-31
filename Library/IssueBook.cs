using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Library
{
    public partial class IssueBook : Form
    {
        private int selectedBookId = -1;

        public IssueBook()
        {
            InitializeComponent();
            LoadIssuedBooks(); // Show issued books when form loads
        }

        private void homeBtn_Click(object sender, EventArgs e)
        {
            MainForm main = new MainForm();
            this.Hide();
            main.ShowDialog();
            this.Show();
        }

        private void addBookBtn_Click(object sender, EventArgs e)
        {
            AddBook form = new AddBook();
            this.Hide();
            form.ShowDialog();
            this.Show();
        }

        private void returnBookBtn_Click(object sender, EventArgs e)
        {
            ReturnBook form = new ReturnBook();
            this.Hide();
            form.ShowDialog();
            this.Show();
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

        private void btnSearchBook_Click(object sender, EventArgs e)
        {
            string searchTitle = txtSearchTitle.Text.Trim();

            if (searchTitle == "")
            {
                MessageBox.Show("Enter a book title to search.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                string query = "SELECT * FROM books WHERE title = @title";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", searchTitle);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtBookTitle.Text = reader["title"].ToString();
                            txtAuthor.Text = reader["author"].ToString();
                            txtQuantity.Text = reader["quantity"].ToString();

                            if (reader["cover"] != DBNull.Value)
                            {
                                byte[] imageBytes = (byte[])reader["cover"];
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    pictureBoxCover.Image = Image.FromStream(ms);
                                    pictureBoxCover.SizeMode = PictureBoxSizeMode.StretchImage;
                                }
                            }
                            else
                            {
                                pictureBoxCover.Image = null;
                            }

                            selectedBookId = Convert.ToInt32(reader["id"]);
                        }
                        else
                        {
                            MessageBox.Show("Book not found.");
                            ClearForm();
                        }
                    }
                }
            }
        }

        private void btnIssue_Click(object sender, EventArgs e)
        {
            if (selectedBookId == -1)
            {
                MessageBox.Show("Search and select a book first.");
                return;
            }

            if (txtPersonName.Text == "" || txtPersonID.Text == "" || txtPhone.Text == "")
            {
                MessageBox.Show("Please fill all borrower details.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                // Insert issued book record
                string insertQuery = @"INSERT INTO issued_books 
                    (book_id, book_title, student_name, student_id, phone, issue_date, return_date) 
                    VALUES (@book_id, @book_title, @student_name, @student_id, @phone, @issue_date, @return_date)";

                using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@book_id", selectedBookId);
                    cmd.Parameters.AddWithValue("@book_title", txtBookTitle.Text);
                    cmd.Parameters.AddWithValue("@student_name", txtPersonName.Text);
                    cmd.Parameters.AddWithValue("@student_id", txtPersonID.Text);
                    cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                    cmd.Parameters.AddWithValue("@issue_date", dateIssue.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@return_date", dateReturn.Value.ToString("yyyy-MM-dd"));

                    cmd.ExecuteNonQuery();
                }

                // Decrease quantity
                string updateQty = "UPDATE books SET quantity = quantity - 1 WHERE id = @id AND quantity > 0";
                using (SQLiteCommand cmd2 = new SQLiteCommand(updateQty, conn))
                {
                    cmd2.Parameters.AddWithValue("@id", selectedBookId);
                    cmd2.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Book issued successfully!");
            ClearForm();
            LoadIssuedBooks();
        }

        private void LoadIssuedBooks()
        {
            using (var conn = DB.GetConnection())
            {
                string query = @"SELECT id, book_title, student_name, student_id, phone, issue_date, return_date
                                 FROM issued_books
                                 ORDER BY id DESC";

                using (SQLiteDataAdapter da = new SQLiteDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dataGridIssuedBooks.DataSource = dt;
                }
            }
        }

        private void ClearForm()
        {
            txtSearchTitle.Clear();
            txtBookTitle.Clear();
            txtAuthor.Clear();
            txtQuantity.Clear();
            txtPersonName.Clear();
            txtPersonID.Clear();
            txtPhone.Clear();
            pictureBoxCover.Image = null;
            dateIssue.Value = DateTime.Today;
            dateReturn.Value = DateTime.Today.AddDays(7);
            selectedBookId = -1;
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadIssuedBooks(); // Refresh every time form is shown
        }

    }
}
