// ================================================================================
// 檔案：/PendingApproval.aspx.cs (新增)
// 說明：此頁面的後端程式碼，負責從資料庫載入待簽核的 SR 資料。
// ================================================================================
using System;
using System.Data;
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
                    sr.Purpose, 
                    s.StatusName, 
                    u_req.Username AS RequestorName, 
                    ISNULL(u_eng.Username, 'N/A') AS EngineerName
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_Users_DEFINE u_req ON sr.RequestorUserID = u_req.UserID
                LEFT JOIN ASE_BPCIM_SR_Users_DEFINE u_eng ON sr.AssignedEngineerID = u_eng.UserID
                JOIN ASE_BPCIM_SR_Approvers_HIS sra ON sr.SRID = sra.SRID
                WHERE sra.ApproverUserID = {currentUserId} 
                AND sra.ApprovalStatus = N'待簽核' 
                AND sra.ApproverType = 'To'
                ORDER BY sr.SubmitDate ASC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvPendingApproval.DataSource = dt;
            gvPendingApproval.DataBind();
        }
    }
}
