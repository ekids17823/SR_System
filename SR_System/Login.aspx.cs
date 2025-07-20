// ================================================================================
// 檔案：/Login.aspx.cs
// 功能：處理使用者登入、驗證、角色判斷與資料同步。
// 變更：簡化了 ProvisionUserAndSetSession，不再需要查詢 Users_DEFINE 來獲取 UserID。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Security;
using System.Web.UI;
using SR_System.DAL;

namespace SR_System
{
    public partial class Login : Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (this.Master is SiteMaster master)
            {
                master.SidebarVisible = false;
            }

            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Default.aspx");
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string employeeID = txtEmployeeID.Text.Trim();
            string password = txtPassword.Text;

            bool isAuthenticatedByWebService = AuthenticateWithWebService(employeeID, password);

            if (isAuthenticatedByWebService)
            {
                if (ProvisionUserAndSetSession(employeeID))
                {
                    FormsAuthentication.SetAuthCookie(employeeID, false);
                    Response.Redirect(FormsAuthentication.GetRedirectUrl(employeeID, false));
                }
                else
                {
                    litMessage.Text = "<div class='alert alert-danger'>登入成功，但同步使用者資料時發生錯誤。請聯絡管理員。</div>";
                }
            }
            else
            {
                litMessage.Text = "<div class='alert alert-danger'>登入失敗，請檢查您的工號和密碼。</div>";
            }
        }

        private bool AuthenticateWithWebService(string employeeID, string password)
        {
            var mockWsUsers = new Dictionary<string, string>
            {
                { "admin", "password123" }, { "user001", "password123" }, { "manager001", "password123" },
                { "signoff01", "password123" }, { "signmanager01", "password123" },
                { "cim_eng_a", "password123" }, { "cim_eng_b", "password123" }, { "cim_eng_c", "password123" },
                { "leader1", "password123" }, { "leader2", "password123" },
                { "boss1", "password123" }, { "boss2", "password123" }
            };
            return mockWsUsers.ContainsKey(employeeID) && mockWsUsers[employeeID] == password;
        }

        private bool ProvisionUserAndSetSession(string employeeID)
        {
            string sanitizedEmployeeID = employeeID.Replace("'", "''");

            string yellowPagesQuery = $"SELECT * FROM ASE_BPCIM_SR_YellowPages_TEST WHERE EmployeeID = N'{sanitizedEmployeeID}'";
            DataTable ypDt = sqlConnect.Get_Table_DATA("DefaultConnection", yellowPagesQuery);

            if (ypDt.Rows.Count == 0)
            {
                litMessage.Text = "<div class='alert alert-warning'>您的帳號存在，但未在公司黃頁中找到對應資料，請聯絡 IT。</div>";
                return false;
            }
            DataRow ypRow = ypDt.Rows[0];

            // 確保使用者存在於 Users_DEFINE 表中
            int userId = FindOrCreateUserInSystem(employeeID);

            Session["UserID"] = userId;
            Session["Username"] = ypRow["Username"].ToString();
            Session["RoleName"] = ypRow["Position"].ToString();
            Session["EmployeeID"] = employeeID;
            Session["Department"] = ypRow["Department"].ToString();

            UpdateLastLogin(userId);
            return true;
        }

        private int FindOrCreateUserInSystem(string employeeId)
        {
            string sanitizedEmployeeId = employeeId.Replace("'", "''");
            string userQuery = $"SELECT UserID FROM ASE_BPCIM_SR_Users_DEFINE WHERE EmployeeID = N'{sanitizedEmployeeId}'";
            object userIdObj = sqlConnect.Execute_Scalar("DefaultConnection", userQuery);

            if (userIdObj != null)
            {
                return Convert.ToInt32(userIdObj);
            }
            else
            {
                string insertUserQuery = $"INSERT INTO ASE_BPCIM_SR_Users_DEFINE (EmployeeID) OUTPUT INSERTED.UserID VALUES (N'{sanitizedEmployeeId}');";
                return (int)sqlConnect.Execute_Scalar("DefaultConnection", insertUserQuery);
            }
        }

        private void UpdateLastLogin(int userId)
        {
            string query = $"UPDATE ASE_BPCIM_SR_Users_DEFINE SET LastLoginDate = GETDATE() WHERE UserID = {userId}";
            sqlConnect.Insert_Table_DATA("DefaultConnection", query);
        }
    }
}
