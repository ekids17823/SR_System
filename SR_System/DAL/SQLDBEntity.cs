// ================================================================================
// 檔案：/DAL/SQLDBEntity.cs
// 功能：提供一個集中的資料庫存取層，用於執行 SQL 命令。
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
        /// <summary>
        /// 執行 SQL 查詢並回傳 DataTable。
        /// </summary>
        /// <param name="sSourceDB">Web.config 中的連線字串名稱。</param>
        /// <param name="sSqlCmd">完整的 SQL 命令字串。</param>
        /// <returns>查詢結果的 DataTable。</returns>
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
        /// <param name="sSourceDB">Web.config 中的連線字串名稱。</param>
        /// <param name="sSqlCmd">完整的 SQL 命令字串。</param>
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
        /// <param name="sSourceDB">Web.config 中的連線字串名稱。</param>
        /// <param name="sSqlCmd">完整的 SQL 命令字串。</param>
        /// <returns>結果集的第一個資料列的第一個資料行。</returns>
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
