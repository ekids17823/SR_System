// ================================================================================
// 檔案：/History.aspx.cs (新增)
// 說明：此頁面的後端程式碼，負責載入使用者的開單歷史紀錄。
// ================================================================================
using System;
using System.Data;
using System.Web.UI;
using SR_System.DAL;

namespace SR_System
{
    public partial class History : System.Web.UI.Page
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
