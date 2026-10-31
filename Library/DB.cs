using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Library
{
    public static class DB
    {
        private static readonly string connectionString = "Data Source=Library.db;Version=3;";

        public static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(connectionString);
            conn.Open(); // Always open before returning
            return conn;
        }
    }
}


