using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters.Binary;

namespace LegacyErp.Data
{
    public class OrderRepository
    {
        private SqlConnection _conn; // ISSUE: shared cached connection

        public OrderRepository(string cs)
        {
            _conn = new SqlConnection(cs);
            _conn.Open(); // ISSUE: opened once, held open, never disposed
        }

        // ISSUE: SQL injection via string concatenation
        public DataTable SearchOrders(string customerName)
        {
            string sql = "SELECT * FROM Orders WHERE CustomerName = '" + customerName + "'";
            SqlCommand cmd = new SqlCommand(sql, _conn); // ISSUE: no using
            SqlDataReader reader = cmd.ExecuteReader();   // ISSUE: no using, sync
            DataTable dt = new DataTable();

            while (reader.Read())
            {
                // ISSUE: GetOrdinal inside loop + same value read multiple times + indexer boxing
                decimal total = (decimal)reader[reader.GetOrdinal("Total")];
                if ((decimal)reader[reader.GetOrdinal("Total")] > 100) // read again
                {
                    Console.WriteLine((decimal)reader[reader.GetOrdinal("Total")]); // and again
                }
                // ISSUE: direct cast, throws on DBNull
                string name = (string)reader["CustomerName"];
            }
            return dt; // reader/cmd never disposed
        }

        // ISSUE: two writes that must be atomic, no transaction
        public void TransferStock(int fromId, int toId, int qty)
        {
            var c1 = new SqlCommand("UPDATE Stock SET Qty = Qty - " + qty + " WHERE Id = " + fromId, _conn);
            c1.ExecuteNonQuery();
            var c2 = new SqlCommand("UPDATE Stock SET Qty = Qty + " + qty + " WHERE Id = " + toId, _conn);
            c2.ExecuteNonQuery();
        }

        // ISSUE: BinaryFormatter (insecure + removed on modern .NET)
        public byte[] Serialize(object o)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, o);
                return ms.ToArray();
            }
        }
    }
}
