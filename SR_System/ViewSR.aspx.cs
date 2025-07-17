// ================================================================================
// 檔案：/ViewSR.aspx.cs
// 功能：顯示單一 SR 的詳細資訊，並根據使用者角色和 SR 狀態提供對應的操作。
// 變更：1. 修正了 BindEngineers 的查詢，使其從黃頁獲取 UserID。
//       2. 修正了 btnAssign_Click 的邏輯，使其能正確地將 UserID 轉換為 EmployeeID。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using SR_System.DAL;
using SR_System.Helpers;

namespace SR_System
{
    public partial class ViewSR : System.Web.UI.Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        private int SRID { get { return Convert.ToInt32(Request.QueryString["SRID"]); } }
        private string CurrentEmployeeID { get { return Session["EmployeeID"].ToString(); } }
        private string CurrentUserRole { get { return Session["RoleName"].ToString(); } }
        private int CurrentUserID { get { return (int)Session["UserID"]; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                if (User.Identity.IsAuthenticated) FormsAuthentication.SignOut();
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadSRDetails();
            }
        }

        private void LoadSRDetails()
        {
            string query = $@"
                SELECT sr.*, s.StatusName, 
                       u_req_yp.Username AS RequestorName, 
                       sr.RequestorEmployeeID, 
                       u_eng_yp.Username AS EngineerName
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_YellowPages_TEST u_req_yp ON sr.RequestorEmployeeID = u_req_yp.EmployeeID
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST u_eng_yp ON sr.AssignedEngineerEmployeeID = u_eng_yp.EmployeeID
                WHERE sr.SRID = {SRID}";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                lblSrNumber.Text = row["SR_Number"].ToString();
                lblTitle.Text = row["Title"].ToString();
                lblStatus.Text = row["StatusName"].ToString();
                lblRequestor.Text = $"{row["RequestorName"]} ({row["RequestorEmployeeID"]})";
                lblSubmitDate.Text = Convert.ToDateTime(row["SubmitDate"]).ToString("yyyy-MM-dd");

                litPurpose.Text = HttpUtility.HtmlEncode(row["Purpose"].ToString()).Replace("\n", "<br />");
                litScope.Text = HttpUtility.HtmlEncode(row["Scope"].ToString()).Replace("\n", "<br />");
                litBenefit.Text = HttpUtility.HtmlEncode(row["Benefit"].ToString()).Replace("\n", "<br />");

                if (row["EngineerName"] != DBNull.Value)
                    lblAssignedEngineer.Text = row["EngineerName"].ToString();
                if (row["PlannedCompletionDate"] != DBNull.Value)
                    lblPlannedCompletionDate.Text = Convert.ToDateTime(row["PlannedCompletionDate"]).ToString("yyyy-MM-dd");

                BindFiles(rptInitialDocs, row["InitialDocPath"].ToString());
                ShowActionPanels(row);
            }
            else
            {
                ShowMessage("找不到指定的 SR。", "danger");
                pnlMainContent.Visible = false;
                return;
            }

            LoadApprovers();
            LoadHistory();
        }

        private void ShowActionPanels(DataRow srData)
        {
            string status = srData["StatusName"].ToString();
            string requestorEmployeeId = srData["RequestorEmployeeID"].ToString();
            string engineerEmployeeId = srData["AssignedEngineerEmployeeID"] == DBNull.Value ? "" : srData["AssignedEngineerEmployeeID"].ToString();
            string cimGroup = srData["CIM_Group"].ToString();

            if (status == "待開單主管審核" && IsCurrentUserRequesterManager(requestorEmployeeId)) pnlRequesterManagerAction.Visible = true;
            if (status == "待會簽審核" && IsCurrentUserASignOffApprover()) pnlSignOffAction.Visible = true;
            if (status == "待CIM主管審核" && IsCurrentUserCimBoss(cimGroup)) pnlCimBossAction.Visible = true;
            if (status == "待CIM主任指派" && IsCurrentUserCimLeader(cimGroup))
            {
                pnlCimLeaderAction.Visible = true;
                BindEngineers(cimGroup);
            }
            if (CurrentEmployeeID == engineerEmployeeId)
            {
                pnlEngineerAction.Visible = true;
                btnAcceptSR.Visible = (status == "待工程師接單");
                btnCompleteDev.Visible = (status == "開發中");
                btnDeploy.Visible = (status == "待使用者上傳報告");
                btnConfirmClosure.Visible = (status == "待程式上線");
            }
            if (CurrentEmployeeID == requestorEmployeeId && status == "待使用者測試") pnlUserAction.Visible = true;
        }

        #region Role & Approver Checks
        private bool IsCurrentUserRequesterManager(string requestorEmployeeId)
        {
            string managerQuery = $"SELECT ManagerEmployeeID FROM ASE_BPCIM_SR_YellowPages_TEST WHERE EmployeeID = N'{requestorEmployeeId.Replace("'", "''")}'";
            string managerId = sqlConnect.Execute_Scalar("DefaultConnection", managerQuery)?.ToString();
            return !string.IsNullOrEmpty(managerId) && managerId.Equals(this.CurrentEmployeeID, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCurrentUserASignOffApprover()
        {
            string query = $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApproverEmployeeID = N'{CurrentEmployeeID}' AND ApprovalStatus = N'待簽核'";
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", query) > 0;
        }

        private bool IsCurrentUserCimBoss(string cimGroup)
        {
            string sanitizedGroup = cimGroup.Replace("'", "''");
            string query = $"SELECT COUNT(1) FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = N'{sanitizedGroup}' AND (Boss1EmployeeID = N'{CurrentEmployeeID}' OR Boss2EmployeeID = N'{CurrentEmployeeID}')";
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", query) > 0;
        }

        private bool IsCurrentUserCimLeader(string cimGroup)
        {
            string sanitizedGroup = cimGroup.Replace("'", "''");
            string query = $"SELECT COUNT(1) FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = N'{sanitizedGroup}' AND LeaderEmployeeID = N'{CurrentEmployeeID}'";
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", query) > 0;
        }
        #endregion

        #region Data Loading & Binding
        private void LoadApprovers()
        {
            string query = $@"
                SELECT yp.Username, sra.ApproverEmployeeID AS EmployeeID, sra.ApprovalStatus, sra.ApprovalDate, sra.Comments
                FROM ASE_BPCIM_SR_Approvers_HIS sra
                JOIN ASE_BPCIM_SR_YellowPages_TEST yp ON sra.ApproverEmployeeID = yp.EmployeeID
                WHERE sra.SRID = {SRID} ORDER BY sra.SRAID";
            gvApprovers.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvApprovers.DataBind();
            pnlApproverList.Visible = gvApprovers.Rows.Count > 0;
        }

        private void LoadHistory()
        {
            string query = $@"
                SELECT h.ActionDate, yp.Username, h.Action, h.Notes
                FROM ASE_BPCIM_SR_Action_HIS h
                JOIN ASE_BPCIM_SR_YellowPages_TEST yp ON h.ActionByEmployeeID = yp.EmployeeID
                WHERE h.SRID = {SRID} ORDER BY h.ActionDate ASC";
            gvHistory.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvHistory.DataBind();
        }

        private void BindEngineers(string cimGroup)
        {
            string sanitizedGroup = cimGroup.Replace("'", "''");
            string query = $"SELECT u.UserID, yp.Username FROM ASE_BPCIM_SR_Users_DEFINE u JOIN ASE_BPCIM_SR_YellowPages_TEST yp ON u.EmployeeID = yp.EmployeeID WHERE yp.Department = 'CIM' AND yp.CIM_Group = N'{sanitizedGroup}' AND yp.Position = N'工程師'";
            ddlEngineers.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            ddlEngineers.DataTextField = "Username";
            ddlEngineers.DataValueField = "UserID";
            ddlEngineers.DataBind();
            ddlEngineers.Items.Insert(0, new ListItem("-- 請選擇 --", "0"));
        }

        private void BindFiles(Repeater repeater, string fileData)
        {
            if (!string.IsNullOrEmpty(fileData))
            {
                var files = fileData.Split(';')
                    .Where(f => !string.IsNullOrEmpty(f) && f.Contains("|"))
                    .Select(f => {
                        var parts = f.Split('|');
                        return new
                        {
                            UniqueFileName = parts[0],
                            OriginalFileName = parts[1]
                        };
                    }).ToList();

                repeater.DataSource = files;
                repeater.DataBind();
            }
        }
        #endregion

        #region Action Handlers
        protected void btnReqManagerApprove_Click(object sender, EventArgs e) { string nextStatus = HasSignOffApprovers() ? "待會簽審核" : "待CIM主管審核"; int nextStatusId = GetStatusId(nextStatus); UpdateSRStatus(nextStatusId, "開單主管同意", txtActionComments.Text.Trim()); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET RequesterManagerApprovalStatus = 'Approved', RequesterManagerApprovalDate = GETDATE() WHERE SRID = {SRID}"); NotificationHelper.SendNotificationForStatusChange(SRID, nextStatus); ReloadPage(); }
        protected void btnSignOffApprove_Click(object sender, EventArgs e) { UpdateSignOffApproverStatus("已同意", txtActionComments.Text.Trim()); if (AreAllSignOffApproversDone()) { UpdateSRStatus(GetStatusId("待CIM主管審核"), "會簽完成", "所有會簽人員已同意。"); NotificationHelper.SendNotificationForStatusChange(SRID, "待CIM主管審核"); } ReloadPage(); }
        protected void btnCimBossApprove_Click(object sender, EventArgs e) { UpdateSRStatus(GetStatusId("待CIM主任指派"), "CIM主管同意", txtActionComments.Text.Trim()); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET CIMBossApprovalStatus = 'Approved', CIMBossApprovalDate = GETDATE() WHERE SRID = {SRID}"); NotificationHelper.SendNotificationForStatusChange(SRID, "待CIM主任指派"); ReloadPage(); }
        protected void btnAssign_Click(object sender, EventArgs e)
        {
            if (ddlEngineers.SelectedValue == "0") { ShowMessage("請選擇一位工程師。", "warning"); return; }
            string plannedDateValue = string.IsNullOrEmpty(txtPlannedDate.Text) ? "NULL" : $"'{txtPlannedDate.Text}'";
            string engineerEmployeeId = GetEmployeeIdByUserId(Convert.ToInt32(ddlEngineers.SelectedValue));
            if (string.IsNullOrEmpty(engineerEmployeeId)) { ShowMessage("找不到對應的工程師工號。", "danger"); return; }
            UpdateSRStatus(GetStatusId("待工程師接單"), "CIM主任指派", $"指派給工程師: {ddlEngineers.SelectedItem.Text}");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"UPDATE ASE_BPCIM_SR_HIS SET AssignedEngineerEmployeeID = N'{engineerEmployeeId}', AssignmentDate = GETDATE(), PlannedCompletionDate = {plannedDateValue} WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師接單");
            ReloadPage();
        }
        protected void btnAcceptSR_Click(object sender, EventArgs e) { UpdateSRStatus(GetStatusId("開發中"), "工程師接單", "工程師已確認接單。"); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerAcceptanceDate = GETDATE() WHERE SRID = {SRID}"); NotificationHelper.SendNotificationForStatusChange(SRID, "開發中"); ReloadPage(); }
        protected void btnCompleteDev_Click(object sender, EventArgs e) { UpdateSRStatus(GetStatusId("待使用者測試"), "完成開發", "工程師已完成開發，待使用者測試。"); NotificationHelper.SendNotificationForStatusChange(SRID, "待使用者測試"); ReloadPage(); }
        protected void btnUploadClosureReport_Click(object sender, EventArgs e) { if (!fileUploadClosureReport.HasFile) { ShowMessage("請上傳測試報告。", "warning"); return; } string filePaths = UploadFiles(fileUploadClosureReport); string sanitizedFilePaths = filePaths.Replace("'", "''"); UpdateSRStatus(GetStatusId("待程式上線"), "上傳測試報告", "使用者已上傳測試報告。"); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET ClosureReportPath = N'{sanitizedFilePaths}' WHERE SRID = {SRID}"); NotificationHelper.SendNotificationForStatusChange(SRID, "待程式上線"); ReloadPage(); }
        protected void btnDeploy_Click(object sender, EventArgs e) { UpdateSRStatus(GetStatusId("待工程師結單"), "程式上線", "工程師已將程式上線。"); NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師結單"); ReloadPage(); }
        protected void btnConfirmClosure_Click(object sender, EventArgs e) { UpdateSRStatus(GetStatusId("已結案"), "工程師結單", "工程師已結單，此 SR 已結案。"); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerConfirmClosureDate = GETDATE() WHERE SRID = {SRID}"); NotificationHelper.SendNotificationForStatusChange(SRID, "已結案"); ReloadPage(); }
        protected void btnAction_Reject(object sender, EventArgs e) { string reason = string.IsNullOrEmpty(txtActionComments.Text) ? "無" : txtActionComments.Text.Replace("'", "''"); UpdateSRStatus(GetStatusId("已拒絕"), "需求被駁回", $"操作者: {Session["Username"]}，原因: {reason}"); NotificationHelper.SendNotificationForStatusChange(SRID, "已拒絕"); ReloadPage(); }
        #endregion

        #region Helper Methods
        private string GetEmployeeIdByUserId(int userId) { return sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT EmployeeID FROM ASE_BPCIM_SR_Users_DEFINE WHERE UserID = {userId}")?.ToString(); }
        private void UpdateSRStatus(int newStatusId, string action, string notes) { int oldStatusId = GetCurrentStatusId(); sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET CurrentStatusID = {newStatusId} WHERE SRID = {SRID}"); AddHistory(action, notes, oldStatusId, newStatusId); }
        private void UpdateSignOffApproverStatus(string status, string comments) { string sanitizedComments = comments.Replace("'", "''"); sqlConnect.Insert_Table_DATA("DefaultConnection", $@"UPDATE ASE_BPCIM_SR_Approvers_HIS SET ApprovalStatus = N'{status}', Comments = N'{sanitizedComments}', ApprovalDate = GETDATE() WHERE SRID = {SRID} AND ApproverEmployeeID = N'{CurrentEmployeeID}'"); AddHistory("會簽人員簽核", $"簽核狀態: {status}。意見: {sanitizedComments}"); }
        private bool HasSignOffApprovers() { return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID}") > 0; }
        private bool AreAllSignOffApproversDone() { return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApprovalStatus = '待簽核'") == 0; }
        private void AddHistory(string action, string notes, int? oldStatusId = null, int? newStatusId = null) { string sanitizedAction = action.Replace("'", "''"); string sanitizedNotes = notes.Replace("'", "''"); string oldStatusValue = oldStatusId.HasValue ? oldStatusId.Value.ToString() : "NULL"; string newStatusValue = newStatusId.HasValue ? newStatusId.Value.ToString() : "NULL"; sqlConnect.Insert_Table_DATA("DefaultConnection", $@"INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByEmployeeID, Notes, OldStatusID, NewStatusID) VALUES ({SRID}, N'{sanitizedAction}', N'{CurrentEmployeeID}', N'{sanitizedNotes}', {oldStatusValue}, {newStatusValue})"); }
        private int GetCurrentStatusId() { return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT CurrentStatusID FROM ASE_BPCIM_SR_HIS WHERE SRID = {SRID}"); }
        private int GetStatusId(string statusName) { string sanitizedStatusName = statusName.Replace("'", "''"); return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT StatusID FROM ASE_BPCIM_SR_Statuses_DEFINE WHERE StatusName = N'{sanitizedStatusName}'"); }
        private string UploadFiles(FileUpload fileUploadControl) { if (fileUploadControl.HasFile) { string originalFileName = Path.GetFileName(fileUploadControl.PostedFile.FileName); string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName; string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]); if (!Directory.Exists(uploadFolder)) { Directory.CreateDirectory(uploadFolder); } string filePath = Path.Combine(uploadFolder, uniqueFileName); fileUploadControl.SaveAs(filePath); return $"{uniqueFileName}|{originalFileName}"; } return null; }
        private void ShowMessage(string message, string type) { litMessage.Text = $"<div class='alert alert-{type}'>{message}</div>"; }
        private void ReloadPage() { Response.Redirect(Request.RawUrl); }
        protected string GetStatusBadgeClass(string status) { switch (status) { case "已同意": return "bg-success"; case "已拒絕": return "bg-danger"; case "待簽核": return "bg-warning text-dark"; default: return "bg-secondary"; } }
        #endregion
    }
}
