// ================================================================================
// 檔案：/DAL/SQLDBEntity.cs
// 變更：1. 將 Execute_NonQuery 方法重新命名為您指定的 Insert_Table_DATA。
//       2. 保留 Get_Table_DATA 和 Execute_Scalar 方法以應對不同查詢需求。
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

        /// <summary>
        /// 執行不回傳結果的 SQL 命令 (INSERT, UPDATE, DELETE)。
        /// </summary>
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

        /// <summary>
        /// 執行查詢，並回傳查詢所傳回之結果集中第一個資料列的第一個資料行。
        /// </summary>
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
