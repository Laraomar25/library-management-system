using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace Library
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void addBookBtn_Click(object sender, EventArgs e)
        {
            AddBook form = new AddBook();
            this.Hide();
            form.ShowDialog();  
            this.Show();

        }

        private void issueBookBtn_Click(object sender, EventArgs e)
        {
            IssueBook form = new IssueBook();
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
        private void MainForm_Load(object sender, EventArgs e)
        {
          
           
        }
       

        private void btnShowChart_Click(object sender, EventArgs e)
        {
            this.Hide();
            GenreChartForm chartForm = new GenreChartForm();
            chartForm.ShowDialog();
            this.Show();
        }
    }
}
