// ================================================================================
// 檔案：/History.aspx.cs
// 功能：此頁面的後端程式碼，負責載入使用者的開單歷史紀錄。
// 變更：修正了 BindGridView 中的 SQL 查詢，使其完全基於 EmployeeID 進行篩選。
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
            if (Session["EmployeeID"] == null)
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
            string currentEmployeeId = Session["EmployeeID"].ToString().Replace("'", "''");
            string query = $@"
                SELECT 
                    sr.SRID, 
                    sr.Title, 
                    s.StatusName, 
                    ISNULL(u_eng_yp.Username, 'N/A') AS EngineerName,
                    sr.SubmitDate,
                    sr.EngineerConfirmClosureDate
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST u_eng_yp ON sr.AssignedEngineerEmployeeID = u_eng_yp.EmployeeID
                WHERE sr.RequestorEmployeeID = N'{currentEmployeeId}'
                ORDER BY sr.SubmitDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvHistory.DataSource = dt;
            gvHistory.DataBind();
        }
    }
}
