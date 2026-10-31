using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace Library
{
    public partial class RegisterFrom : Form
    {
        private string dbPath = @"Data Source=Library.db;Version=3;";

        public RegisterFrom()
        {
            InitializeComponent();
        }

        private void RegisterFrom_Load(object sender, EventArgs e)
        {
            // Optional: Do something on load
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (username == "" || email == "" || password == "")
            {
                MessageBox.Show("Please fill all the fields", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Step 1: Insert the user into the database
                using (SQLiteConnection conn = new SQLiteConnection(dbPath))
                {
                    conn.Open();

                    string query = "INSERT INTO users (username, email, password) VALUES (@username, @email, @password)";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", password); // Optional: Hash this
                        cmd.ExecuteNonQuery();
                    }
                }

                // Step 2: Show success message and open login form AFTER connection is closed
                MessageBox.Show("Registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Hide();
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGoToLogin_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
            this.Hide();
        }
    }
}
