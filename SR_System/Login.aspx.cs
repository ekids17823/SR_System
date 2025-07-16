// ================================================================================
// 檔案：/Login.aspx.cs
// 說明：處理使用者登入邏輯。
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
            // 在 Master Page 中找到 Sidebar 並將其隱藏
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
                { "admin", "password123" }, { "user001", "password123" }, { "eng001", "password123" },
                { "eng002", "password123" }, { "l1_supervisor", "password123" }, { "l2_supervisor", "password123" },
                { "signoff01", "password123" }, { "signoff02", "password123" }, { "newuser999", "password123" }
            };
            return mockWsUsers.ContainsKey(employeeID) && mockWsUsers[employeeID] == password;
        }

        private bool ProvisionUserAndSetSession(string employeeID)
        {
            int userId = 0;
            string username = string.Empty;
            string roleName = string.Empty;

            string sanitizedEmployeeID = employeeID.Replace("'", "''");
            string query = $"SELECT u.UserID, u.Username, r.RoleName FROM ASE_BPCIM_SR_Users_DEFINE u JOIN ASE_BPCIM_SR_Roles_DEFINE r ON u.RoleID = r.RoleID WHERE u.EmployeeID = N'{sanitizedEmployeeID}' AND u.IsActive = 1";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                userId = Convert.ToInt32(dt.Rows[0]["UserID"]);
                username = dt.Rows[0]["Username"].ToString();
                roleName = dt.Rows[0]["RoleName"].ToString();
            }
            else
            {
                string defaultRole = "User";
                string defaultUsername = sanitizedEmployeeID;

                string insertQuery = $@"
                    INSERT INTO ASE_BPCIM_SR_Users_DEFINE (Username, EmployeeID, RoleID) 
                    VALUES (N'{defaultUsername}', N'{sanitizedEmployeeID}', (SELECT RoleID FROM ASE_BPCIM_SR_Roles_DEFINE WHERE RoleName = N'{defaultRole}'));";

                sqlConnect.Insert_Table_DATA("DefaultConnection", insertQuery);

                DataTable newUserDt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
                if (newUserDt.Rows.Count > 0)
                {
                    userId = Convert.ToInt32(newUserDt.Rows[0]["UserID"]);
                    username = newUserDt.Rows[0]["Username"].ToString();
                    roleName = newUserDt.Rows[0]["RoleName"].ToString();
                }
                else
                {
                    return false;
                }
            }

            Session["UserID"] = userId;
            Session["Username"] = username;
            Session["RoleName"] = roleName;
            Session["EmployeeID"] = employeeID;

            UpdateLastLogin(userId);
            return true;
        }

        private void UpdateLastLogin(int userId)
        {
            string query = $"UPDATE ASE_BPCIM_SR_Users_DEFINE SET LastLoginDate = GETDATE() WHERE UserID = {userId}";
            sqlConnect.Insert_Table_DATA("DefaultConnection", query);
        }
    }
}
