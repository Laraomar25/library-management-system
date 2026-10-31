using System;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Library
{
    public partial class GenreChartForm : Form
    {
        public GenreChartForm()
        {
            InitializeComponent();
            LoadGenreChart();
        }

        private void LoadGenreChart()
        {
            chartGenres.Series.Clear();
            chartGenres.Titles.Clear();
            chartGenres.ChartAreas[0].AxisX.Interval = 1;

            chartGenres.Titles.Add("Books by Genre");

            var series = chartGenres.Series.Add("Genres");
            series.ChartType = SeriesChartType.Column;
            series.IsValueShownAsLabel = true;

            using (var conn = DB.GetConnection())
            {
                string query = "SELECT genre, COUNT(*) as count FROM books GROUP BY genre";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string genre = reader["genre"].ToString();
                            int count = Convert.ToInt32(reader["count"]);
                            series.Points.AddXY(genre, count);
                        }
                    }
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
