using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;

namespace FIRManagementSystem.DataAccess
{
    public class OracleDbHelper
    {
        public static OracleConnection GetConnection()
        {
            
            string connectionString = ConfigurationManager.ConnectionStrings["Task_DB"].ConnectionString;
            var conn = new OracleConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}