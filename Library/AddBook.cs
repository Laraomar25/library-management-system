using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Library
{
    public partial class AddBook : Form
    {
        string selectedImagePath = "";
        private int selectedBookId = -1;

        public AddBook()
        {
            InitializeComponent();
            LoadBooks();
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Book Cover";
                dlg.Filter = "Image Files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    selectedImagePath = dlg.FileName;
                    pictureBox2.Image = Image.FromFile(selectedImagePath);
                    pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }
        }

        private void LoadBooks()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    string query = "SELECT id, title, author, genre, quantity FROM books";
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView1.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading books: " + ex.Message);
            }
        }

        private void ClearForm()
        {
            txtTitle.Clear();
            txtAuthor.Clear();
            txtGenre.Clear();
            txtQuantity.Clear();
            pictureBox2.Image = null;
            selectedImagePath = "";
            selectedBookId = -1;
        }

        private void homeBtn_Click(object sender, EventArgs e)
        {
            MainForm main = new MainForm();
            this.Hide();
            main.ShowDialog();
            this.Show();
        }

        private void issueBookBtn_Click(object sender, EventArgs e)
        {
            IssueBook issue = new IssueBook();
            this.Hide();
            issue.ShowDialog();
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtTitle.Text == "" || txtAuthor.Text == "" || txtGenre.Text == "" || txtQuantity.Text == "")
                {
                    MessageBox.Show("Please fill all fields.");
                    return;
                }

                byte[] imageBytes = null;
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    imageBytes = File.ReadAllBytes(selectedImagePath);
                }

                using (var conn = DB.GetConnection())
                {
                    string query = @"INSERT INTO books (title, author, genre, quantity, cover) 
                                     VALUES (@title, @author, @genre, @quantity, @cover)";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", txtTitle.Text);
                        cmd.Parameters.AddWithValue("@author", txtAuthor.Text);
                        cmd.Parameters.AddWithValue("@genre", txtGenre.Text);
                        cmd.Parameters.AddWithValue("@quantity", int.Parse(txtQuantity.Text));
                        cmd.Parameters.Add("@cover", DbType.Binary).Value = imageBytes ?? (object)DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Book Added!");
                ClearForm();
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedBookId == -1)
            {
                MessageBox.Show("Select a book to delete.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                string query = "DELETE FROM books WHERE id = @id";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", selectedBookId);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Book Deleted.");
            ClearForm();
            LoadBooks();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedBookId == -1)
            {
                MessageBox.Show("Select a book to update.");
                return;
            }

            byte[] imageBytes = null;
            if (!string.IsNullOrEmpty(selectedImagePath))
            {
                imageBytes = File.ReadAllBytes(selectedImagePath);
            }

            try
            {
                using (var conn = DB.GetConnection())
                {
                    string query = @"UPDATE books 
                                     SET title = @title, author = @author,
                                         genre = @genre, quantity = @quantity" +
                                    (imageBytes != null ? ", cover = @cover" : "") +
                                    " WHERE id = @id";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", txtTitle.Text);
                        cmd.Parameters.AddWithValue("@author", txtAuthor.Text);
                        cmd.Parameters.AddWithValue("@genre", txtGenre.Text);
                        cmd.Parameters.AddWithValue("@quantity", int.Parse(txtQuantity.Text));
                        if (imageBytes != null)
                            cmd.Parameters.Add("@cover", DbType.Binary).Value = imageBytes;
                        cmd.Parameters.AddWithValue("@id", selectedBookId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Book Updated.");
                ClearForm();
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating book: " + ex.Message);
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedBookId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["id"].Value);
                txtTitle.Text = dataGridView1.Rows[e.RowIndex].Cells["title"].Value.ToString();
                txtAuthor.Text = dataGridView1.Rows[e.RowIndex].Cells["author"].Value.ToString();
                txtGenre.Text = dataGridView1.Rows[e.RowIndex].Cells["genre"].Value.ToString();
                txtQuantity.Text = dataGridView1.Rows[e.RowIndex].Cells["quantity"].Value.ToString();

                try
                {
                    using (var conn = DB.GetConnection())
                    {
                        string query = "SELECT cover FROM books WHERE id = @id";
                        using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedBookId);
                            var result = cmd.ExecuteScalar();

                            if (result != DBNull.Value && result != null)
                            {
                                byte[] imgBytes = (byte[])result;
                                using (MemoryStream ms = new MemoryStream(imgBytes))
                                {
                                    pictureBox2.Image = Image.FromStream(ms);
                                    pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                                }
                            }
                            else
                            {
                                pictureBox2.Image = null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading cover image: " + ex.Message);
                }
            }
        }
    }
}
