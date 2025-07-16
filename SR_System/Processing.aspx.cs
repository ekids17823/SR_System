// ================================================================================
// 檔案：/Processing.aspx.cs (新增)
// 說明：此頁面的後端程式碼，負責從資料庫載入處理中的 SR 資料。
// ================================================================================
using System;
using System.Data;
using System.Web.UI;
using SR_System.DAL;

namespace SR_System
{
    public partial class Processing : System.Web.UI.Page
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
            string currentUserRole = Session["RoleName"].ToString();

            string query = @"
                SELECT 
                    sr.SRID, 
                    sr.Title, 
                    s.StatusName, 
                    u_req.Username AS RequestorName, 
                    ISNULL(u_eng.Username, 'N/A') AS EngineerName,
                    sr.AssignmentDate
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_Users_DEFINE u_req ON sr.RequestorUserID = u_req.UserID
                LEFT JOIN ASE_BPCIM_SR_Users_DEFINE u_eng ON sr.AssignedEngineerID = u_eng.UserID
                WHERE s.StatusName IN (N'待工程師確認', N'開發中', N'待User上傳結案報告', N'待工程師確認結單') ";

            // 如果是工程師，只看指派給自己的
            if (currentUserRole == "Engineer")
            {
                query += $" AND sr.AssignedEngineerID = {currentUserId}";
            }
            // 主管或管理員則可以看到所有的

            query += " ORDER BY sr.AssignmentDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvProcessing.DataSource = dt;
            gvProcessing.DataBind();
        }
    }
}
