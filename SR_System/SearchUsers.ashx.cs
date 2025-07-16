// ================================================================================
// 檔案：/SearchUsers.ashx.cs
// 變更：所有資料庫存取改為傳遞 string 型別的 SQL 命令。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using SR_System.DAL;

namespace SR_System
{
    public class SearchUsers : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context.Session["UserID"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.StatusDescription = "Unauthorized";
                context.Response.End();
                return;
            }

            string term = (context.Request["term"] ?? "").Replace("'", "''"); // Sanitize
            int currentUserId = (int)context.Session["UserID"];

            List<object> users = new List<object>();
            SQLDBEntity sqlConnect = new SQLDBEntity();

            string query = $"SELECT UserID, Username, EmployeeID FROM ASE_BPCIM_SR_Users_DEFINE WHERE (Username LIKE N'%{term}%' OR EmployeeID LIKE N'%{term}%') AND UserID != {currentUserId} AND IsActive = 1";

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new
                {
                    UserID = row["UserID"],
                    Username = row["Username"].ToString(),
                    EmployeeID = row["EmployeeID"].ToString()
                });
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            context.Response.ContentType = "application/json";
            context.Response.Write(js.Serialize(users));
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
