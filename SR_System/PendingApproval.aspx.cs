// ================================================================================
// 檔案：/PendingApproval.aspx.cs
// 功能：此頁面的後端程式碼，負責從資料庫載入待簽核的 SR 資料。
// 變更：修正了 BindGridView 中的 SQL 查詢，使其完全基於 EmployeeID 進行篩選。
// ================================================================================
using System;
using System.Data;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using SR_System.DAL;

namespace SR_System
{
    public partial class PendingApproval : System.Web.UI.Page
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
                    sr.Purpose, 
                    s.StatusName, 
                    u_req_yp.Username AS RequestorName, 
                    ISNULL(u_eng_yp.Username, 'N/A') AS EngineerName
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_YellowPages_TEST u_req_yp ON sr.RequestorEmployeeID = u_req_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST u_eng_yp ON sr.AssignedEngineerEmployeeID = u_eng_yp.EmployeeID
                JOIN ASE_BPCIM_SR_Approvers_HIS sra ON sr.SRID = sra.SRID
                WHERE sra.ApproverEmployeeID = N'{currentEmployeeId}'
                AND sra.ApprovalStatus = N'待簽核' 
                ORDER BY sr.SubmitDate ASC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvPendingApproval.DataSource = dt;
            gvPendingApproval.DataBind();
        }
    }
}
