// ================================================================================
// 檔案：/History.aspx.cs
// 變更：在 Page_Load 中加入對 Session 的檢查。
// ================================================================================
using System;
using System.Data;
using System.Web.Security;
using System.Web.UI;
using SR_System.DAL;

namespace SR_System
{
    public partial class History : System.Web.UI.Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    FormsAuthentication.SignOut();
                }
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindGridView();
            }
        }

        private void BindGridView()
        {
            int currentUserId = (int)Session["UserID"];
            string query = $@"
                SELECT 
                    sr.SRID, 
                    sr.Title, 
                    s.StatusName, 
                    ISNULL(u_eng.Username, 'N/A') AS EngineerName,
                    sr.SubmitDate,
                    sr.EngineerConfirmClosureDate
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                LEFT JOIN ASE_BPCIM_SR_Users_DEFINE u_eng ON sr.AssignedEngineerID = u_eng.UserID
                WHERE sr.RequestorUserID = {currentUserId}
                ORDER BY sr.SubmitDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvHistory.DataSource = dt;
            gvHistory.DataBind();
        }
    }
}
