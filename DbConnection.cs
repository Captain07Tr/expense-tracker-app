using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;


namespace Gelir_Gider_Projesi
{//   Data Source=.\\MSSQLSERVER02;Initial Catalog=GelirGider;Integrated Security=True;TrustServerCertificate=True
    public static class Database
    {
        public static string connectionString = "Data Source=.\\MSSQLSERVER02;Initial Catalog=GelirGider;Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }

}

