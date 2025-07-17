// ================================================================================
// 檔案：/Processing.aspx.cs
// 功能：此頁面的後端程式碼，負責從資料庫載入所有待處理的 SR 資料。
// 變更：1. 大幅重構了 BindGridView 中的 SQL 查詢，使其能根據不同角色和狀態，
//          動態地找出待處理的案件以及目前的處理人。
//       2. 所有的可視性判斷都已修正為直接比對 EmployeeID，解決了之前的邏輯錯誤。
// ================================================================================
using System;
using System.Data;
using System.Web.Security;
using System.Web.UI;
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
                SELECT 
                    sr.SRID, 
                    sr.SR_Number,
                    sr.Title, 
                    s.StatusName, 
                    req_yp.Username AS RequestorName, 
                    sr.SubmitDate,
                    CASE 
                        WHEN s.StatusName = N'待開單主管審核' THEN req_manager_yp.Username + ' (' + req_manager_yp.EmployeeID + ')'
                        WHEN s.StatusName = N'待會簽審核' THEN N'多位會簽者'
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
                        ELSE 'N/A'
                    END AS CurrentHandler
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_YellowPages_TEST req_yp ON sr.RequestorEmployeeID = req_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST req_manager_yp ON req_yp.ManagerEmployeeID = req_manager_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST eng_yp ON sr.AssignedEngineerEmployeeID = eng_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_CIMLeaders_DEFINE cl ON sr.CIM_Group = cl.CIM_Group
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST leader_yp ON cl.LeaderEmployeeID = leader_yp.EmployeeID
                WHERE 
                    s.StatusName NOT IN (N'已結案', N'已拒絕')
                    AND (
                        -- 1. 開單人員一定看的到
                        sr.RequestorEmployeeID = N'{currentEmployeeId}'
                        -- 2. 開單人員主管也看的到
                        OR req_yp.ManagerEmployeeID = N'{currentEmployeeId}'
                        -- 3. 指派工程師也看的到
                        OR sr.AssignedEngineerEmployeeID = N'{currentEmployeeId}'
                        -- 4. 指派工程師的主管也看的到
                        OR eng_yp.ManagerEmployeeID = N'{currentEmployeeId}'
                        -- 5. & 6. 會簽人員或其主管也看的到
                        OR EXISTS (
                            SELECT 1
                            FROM ASE_BPCIM_SR_Approvers_HIS sra
                            JOIN ASE_BPCIM_SR_YellowPages_TEST sign_yp ON sra.ApproverEmployeeID = sign_yp.EmployeeID
                            WHERE sra.SRID = sr.SRID AND (sra.ApproverEmployeeID = N'{currentEmployeeId}' OR sign_yp.ManagerEmployeeID = N'{currentEmployeeId}')
                        )
                        -- 7. 目前處理人是 CIM 主管
                        OR (s.StatusName = N'待CIM主管審核' AND (cl.Boss1EmployeeID = N'{currentEmployeeId}' OR cl.Boss2EmployeeID = N'{currentEmployeeId}'))
                        -- 8. 目前處理人是 CIM 主任
                        OR (s.StatusName = N'待CIM主任指派' AND cl.LeaderEmployeeID = N'{currentEmployeeId}')
                    )
                ORDER BY sr.SubmitDate DESC";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvProcessing.DataSource = dt;
            gvProcessing.DataBind();
        }
    }
}
