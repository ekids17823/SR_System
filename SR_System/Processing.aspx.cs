// ================================================================================
// 檔案：/Processing.aspx.cs
// 功能：此頁面的後端程式碼，負責從資料庫載入所有待處理的 SR 資料。
// 變更：修正了 gvProcessing_RowDataBound 事件，使其使用 SR_Number 產生連結。
// ================================================================================
using System;
using System.Data;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using SR_System.DAL;

namespace SR_System
{
    public partial class Processing : System.Web.UI.Page
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
                WITH NextApprover AS (
                    SELECT 
                        SRID, 
                        ApproverEmployeeID,
                        ROW_NUMBER() OVER(PARTITION BY SRID ORDER BY SRAID) AS rn
                    FROM ASE_BPCIM_SR_Approvers_HIS
                    WHERE ApprovalStatus = N'待簽核'
                ),
                SR_CTE AS (
                    SELECT 
                        sr.SRID, 
                        sr.SR_Number,
                        sr.Title, 
                        s.StatusName, 
                        req_yp.Username AS RequestorName, 
                        sr.SubmitDate,
                        sr.RequestorEmployeeID,
                        req_yp.ManagerEmployeeID AS RequestorManagerEmployeeID,
                        sr.AssignedEngineerEmployeeID,
                        eng_yp.ManagerEmployeeID AS EngineerManagerEmployeeID,
                        cl.LeaderEmployeeID,
                        cl.Boss1EmployeeID,
                        cl.Boss2EmployeeID,
                        CASE 
                            WHEN s.StatusName = N'待開單主管審核' THEN req_manager_yp.Username + ' (' + req_manager_yp.EmployeeID + ')'
                            WHEN s.StatusName = N'待會簽審核' THEN next_app_yp.Username + ' (' + na.ApproverEmployeeID + ')'
                            WHEN s.StatusName = N'待CIM主管審核' THEN 
                                STUFF((
                                    SELECT ', ' + b_yp.Username + ' (' + b.BossID + ')'
                                    FROM (
                                        SELECT CIM_Group, Boss1EmployeeID AS BossID FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = sr.CIM_Group AND Boss1EmployeeID IS NOT NULL
                                        UNION ALL
                                        SELECT CIM_Group, Boss2EmployeeID AS BossID FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = sr.CIM_Group AND Boss2EmployeeID IS NOT NULL
                                    ) b
                                    JOIN ASE_BPCIM_SR_YellowPages_TEST b_yp ON b.BossID = b_yp.EmployeeID
                                    FOR XML PATH('')
                                ), 1, 2, '')
                            WHEN s.StatusName = N'待CIM主任指派' THEN leader_yp.Username + ' (' + cl.LeaderEmployeeID + ')'
                            WHEN s.StatusName IN (N'待工程師接單', N'開發中', N'待使用者測試', N'待使用者上傳報告', N'待程式上線', N'待工程師結單') THEN eng_yp.Username + ' (' + sr.AssignedEngineerEmployeeID + ')'
                            WHEN s.StatusName = N'待開單人修改' THEN req_yp.Username + ' (' + sr.RequestorEmployeeID + ')'
                            ELSE 'N/A'
                        END AS CurrentHandler
                    FROM ASE_BPCIM_SR_HIS sr
                    JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                    JOIN ASE_BPCIM_SR_YellowPages_TEST req_yp ON sr.RequestorEmployeeID = req_yp.EmployeeID
                    LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST req_manager_yp ON req_yp.ManagerEmployeeID = req_manager_yp.EmployeeID
                    LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST eng_yp ON sr.AssignedEngineerEmployeeID = eng_yp.EmployeeID
                    LEFT JOIN ASE_BPCIM_SR_CIMLeaders_DEFINE cl ON sr.CIM_Group = cl.CIM_Group
                    LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST leader_yp ON cl.LeaderEmployeeID = leader_yp.EmployeeID
                    LEFT JOIN NextApprover na ON sr.SRID = na.SRID AND na.rn = 1
                    LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST next_app_yp ON na.ApproverEmployeeID = next_app_yp.EmployeeID
                    WHERE s.StatusName NOT IN (N'已結案', N'已取消')
                )
                SELECT * FROM SR_CTE
                WHERE 
                    RequestorEmployeeID = N'{currentEmployeeId}'
                    OR RequestorManagerEmployeeID = N'{currentEmployeeId}'
                    OR AssignedEngineerEmployeeID = N'{currentEmployeeId}'
                    OR EngineerManagerEmployeeID = N'{currentEmployeeId}'
                    OR LeaderEmployeeID = N'{currentEmployeeId}'
                    OR Boss1EmployeeID = N'{currentEmployeeId}'
                    OR Boss2EmployeeID = N'{currentEmployeeId}'
                    OR EXISTS (
                        SELECT 1
                        FROM ASE_BPCIM_SR_Approvers_HIS sra
                        JOIN ASE_BPCIM_SR_YellowPages_TEST sign_yp ON sra.ApproverEmployeeID = sign_yp.EmployeeID
                        WHERE sra.SRID = SR_CTE.SRID AND (sra.ApproverEmployeeID = N'{currentEmployeeId}' OR sign_yp.ManagerEmployeeID = N'{currentEmployeeId}')
                    )
                ORDER BY SubmitDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvProcessing.DataSource = dt;
            gvProcessing.DataBind();
        }

        protected void gvProcessing_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                string status = drv["StatusName"].ToString();
                string srNumber = drv["SR_Number"].ToString();
                HyperLink hlSRLink = (HyperLink)e.Row.FindControl("hlSRLink");

                if (hlSRLink != null)
                {
                    if (status == "待開單人修改")
                    {
                        hlSRLink.NavigateUrl = $"~/CreateSR.aspx?SR_Number={srNumber}";
                    }
                    else
                    {
                        hlSRLink.NavigateUrl = $"~/ViewSR.aspx?SR_Number={srNumber}";
                    }
                }
            }
        }
    }
}
