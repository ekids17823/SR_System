// ================================================================================
// 檔案：/EngineerHistory.aspx.cs
// 功能：此頁面的後端程式碼，負責載入 CIM 工程師自己的結單歷史紀錄，並提供篩選功能。
// 變更：1. 在 Page_Load 中加入了嚴謹的權限檢查，確保只有工程師和管理員可以存取。
//       2. 如果權限不足，會跳出提示訊息並導向回首頁。
// ================================================================================
using System;
using System.Data;
using System.Text;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using SR_System.DAL;

namespace SR_System
{
    public partial class EngineerHistory : System.Web.UI.Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        /// <summary>
        /// 頁面載入事件，檢查登入狀態與權限，並繫結資料。
        /// </summary>
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

            // (關鍵修正) 權限檢查
            string userRole = Session["RoleName"]?.ToString() ?? "";
            if (userRole != "工程師" && userRole != "Admin")
            {
                pnlContent.Visible = false; // 隱藏主要內容
                // 使用 ScriptManager 顯示提示訊息後再導向
                string script = "alert('您沒有權限存取此頁面。'); window.location='Default.aspx';";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "AccessDenied", script, true);
                return;
            }

            if (!IsPostBack)
            {
                LoadFilters();
                BindGridView();
            }
        }

        /// <summary>
        /// 載入所有篩選器下拉選單的初始資料。
        /// </summary>
        private void LoadFilters()
        {
            // 載入狀態
            string statusQuery = "SELECT StatusID, StatusName FROM ASE_BPCIM_SR_Statuses_DEFINE ORDER BY StatusID";
            ddlStatus.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", statusQuery);
            ddlStatus.DataTextField = "StatusName";
            ddlStatus.DataValueField = "StatusID";
            ddlStatus.DataBind();
            ddlStatus.Items.Insert(0, new ListItem("所有狀態", ""));

            // 載入組別
            string groupQuery = "SELECT DISTINCT CIM_Group FROM ASE_BPCIM_SR_YellowPages_TEST WHERE CIM_Group IS NOT NULL ORDER BY CIM_Group";
            ddlCimGroup.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", groupQuery);
            ddlCimGroup.DataTextField = "CIM_Group";
            ddlCimGroup.DataValueField = "CIM_Group";
            ddlCimGroup.DataBind();
            ddlCimGroup.Items.Insert(0, new ListItem("所有組別", ""));

            // 載入工程師
            LoadEngineersByGroup(""); // 初始載入所有工程師
        }

        /// <summary>
        /// 根據選擇的組別，動態載入對應的工程師列表。
        /// </summary>
        private void LoadEngineersByGroup(string cimGroup)
        {
            string engineerQuery = "SELECT EmployeeID, Username FROM ASE_BPCIM_SR_YellowPages_TEST WHERE Position = N'工程師'";
            if (!string.IsNullOrEmpty(cimGroup))
            {
                engineerQuery += $" AND CIM_Group = N'{cimGroup.Replace("'", "''")}'";
            }
            engineerQuery += " ORDER BY EmployeeID";

            ddlEngineer.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", engineerQuery);
            ddlEngineer.DataTextField = "Username";
            ddlEngineer.DataValueField = "EmployeeID";
            ddlEngineer.DataBind();
            ddlEngineer.Items.Insert(0, new ListItem("所有工程師", ""));
        }

        /// <summary>
        /// 「查詢」按鈕的點擊事件。
        /// </summary>
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindGridView();
        }

        /// <summary>
        /// 當「組別」下拉選單變更時，重新載入對應的工程師列表。
        /// </summary>
        protected void ddlCimGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadEngineersByGroup(ddlCimGroup.SelectedValue);
        }

        /// <summary>
        /// 從資料庫獲取並繫結符合篩選條件的 SR 列表。
        /// </summary>
        private void BindGridView()
        {
            var whereClauses = new StringBuilder("WHERE 1=1");

            // 處理時間區間
            if (!string.IsNullOrEmpty(txtStartDate.Text))
            {
                whereClauses.Append($" AND sr.SubmitDate >= '{txtStartDate.Text} 00:00:00'");
            }
            if (!string.IsNullOrEmpty(txtEndDate.Text))
            {
                whereClauses.Append($" AND sr.SubmitDate <= '{txtEndDate.Text} 23:59:59'");
            }

            // 處理狀態
            if (!string.IsNullOrEmpty(ddlStatus.SelectedValue))
            {
                whereClauses.Append($" AND sr.CurrentStatusID = {ddlStatus.SelectedValue}");
            }

            // 處理工程師
            if (!string.IsNullOrEmpty(ddlEngineer.SelectedValue))
            {
                whereClauses.Append($" AND sr.AssignedEngineerEmployeeID = N'{ddlEngineer.SelectedValue.Replace("'", "''")}'");
            }
            // 如果沒有選特定工程師，但有選組別，則篩選該組別的所有工程師
            else if (!string.IsNullOrEmpty(ddlCimGroup.SelectedValue))
            {
                whereClauses.Append($" AND eng_yp.CIM_Group = N'{ddlCimGroup.SelectedValue.Replace("'", "''")}'");
            }

            string query = $@"
                SELECT 
                    sr.SRID, 
                    sr.SR_Number,
                    sr.Title, 
                    s.StatusName, 
                    req_yp.Username AS RequestorName,
                    sr.SubmitDate,
                    sr.EngineerConfirmClosureDate
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_YellowPages_TEST req_yp ON sr.RequestorEmployeeID = req_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST eng_yp ON sr.AssignedEngineerEmployeeID = eng_yp.EmployeeID
                {whereClauses}
                ORDER BY sr.SubmitDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvEngineerHistory.DataSource = dt;
            gvEngineerHistory.DataBind();
        }
    }
}
