// ================================================================================
// 檔案：/DAL/SQLDBEntity.cs
// 說明：資料庫存取類別。
// 變更：無變更
// ================================================================================
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SR_System.DAL
{
    public class SQLDBEntity
    {
        public DataTable Get_Table_DATA(string sSourceDB, string sSqlCmd)
        {
            string constr = ConfigurationManager.ConnectionStrings[sSourceDB].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCmd, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public void Insert_Table_DATA(string sSourceDB, string sSqlCmd)
        {
            string constr = ConfigurationManager.ConnectionStrings[sSourceDB].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCmd, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public object Execute_Scalar(string sSourceDB, string sSqlCmd)
        {
            string constr = ConfigurationManager.ConnectionStrings[sSourceDB].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(sSqlCmd, con))
                {
                    con.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}
