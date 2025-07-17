// ================================================================================
// 檔案：/Handlers/SearchUsers.ashx.cs
// 功能：處理使用者搜尋的 AJAX 請求。
// 變更：查詢來源改為黃頁資料表，並移除對 Users_DEFINE 的 JOIN。
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
                context.Response.End();
                return;
            }

            string term = (context.Request["term"] ?? "").Replace("'", "''");
            string roleFilter = context.Request["role"];
            string currentEmployeeID = context.Session["EmployeeID"].ToString();

            List<object> users = new List<object>();
            SQLDBEntity sqlConnect = new SQLDBEntity();

            string query = $@"
                SELECT EmployeeID, Username 
                FROM ASE_BPCIM_SR_YellowPages_TEST 
                WHERE (Username LIKE N'%{term}%' OR EmployeeID LIKE N'%{term}%') 
                AND EmployeeID != N'{currentEmployeeID}'";

            if (!string.IsNullOrEmpty(roleFilter) && roleFilter.Equals("Engineer", StringComparison.OrdinalIgnoreCase))
            {
                query += " AND Position = N'工程師'";
            }

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new
                {
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
