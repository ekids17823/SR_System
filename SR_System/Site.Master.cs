// ================================================================================
// 檔案：/Site.Master.cs
// 功能：網站的共用母版頁後端程式碼。
// ================================================================================
using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SR_System
{
    public partial class SiteMaster : MasterPage
    {
        public bool SidebarVisible
        {
            get { return pnlSidebar.Visible; }
            set
            {
                pnlSidebar.Visible = value;
                pnlToggler.Visible = value;
                if (!value)
                {
                    mainContent.Attributes["class"] = "col-12";
                }
                else
                {
                    mainContent.Attributes["class"] = "col-md-9 ms-sm-auto col-lg-10 px-md-4";
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Context.User.Identity.IsAuthenticated)
                {
                    pnlUserInfo.Visible = true;
                    pnlLogin.Visible = false;
                    lblUsername.Text = Session["Username"]?.ToString() ?? Context.User.Identity.Name;
                    GenerateNavLinks();
                }
                else
                {
                    pnlUserInfo.Visible = false;
                    pnlLogin.Visible = true;
                    this.SidebarVisible = false;
                }
            }
        }

        private void GenerateNavLinks()
        {
            string role = Session["RoleName"]?.ToString() ?? "";
            var navLinks = new System.Text.StringBuilder();

            navLinks.Append(CreateNavItem("~/CreateSR.aspx", "bi-file-earmark-plus-fill", "開單 New SR"));
            navLinks.Append(CreateNavItem("~/Processing.aspx", "bi-gear-fill", "處理中"));
            navLinks.Append(CreateNavItem("~/History.aspx", "bi-clock-history", "開單紀錄"));

            if (role == "Admin")
            {
                navLinks.Append("<h6 class='sidebar-heading d-flex justify-content-between align-items-center px-3 mt-4 mb-1 text-muted'><span>系統管理</span></h6>");
                navLinks.Append(CreateNavItem("~/Admin/ManageUsers.aspx", "bi-people-fill", "管理使用者"));
            }

            litNavLinks.Text = navLinks.ToString();
        }

        private string CreateNavItem(string url, string iconClass, string text)
        {
            string activeClass = (Request.Url.AbsolutePath.EndsWith(url.Replace("~/", ""), StringComparison.OrdinalIgnoreCase)) ? "active" : "";
            return $"<li class='nav-item'><a class='nav-link {activeClass}' href='{ResolveUrl(url)}'><i class='bi {iconClass} me-2'></i>{text}</a></li>";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();
            Response.Redirect("~/Login.aspx");
        }
    }
}
