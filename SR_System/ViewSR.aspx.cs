// ================================================================================
// 檔案：/ViewSR.aspx.cs
// 變更：所有資料庫存取改為使用您指定的 Insert_Table_DATA 方法。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
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
        private int CurrentUserID { get { return (int)Session["UserID"]; } }
        private string CurrentUserRole { get { return Session["RoleName"].ToString(); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadSRDetails();
            }
        }

        private void LoadSRDetails()
        {
            string query = $@"
                SELECT sr.*, s.StatusName, u.Username AS RequestorName, e.Username AS EngineerName
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Statuses_DEFINE s ON sr.CurrentStatusID = s.StatusID
                JOIN ASE_BPCIM_SR_Users_DEFINE u ON sr.RequestorUserID = u.UserID
                LEFT JOIN ASE_BPCIM_SR_Users_DEFINE e ON sr.AssignedEngineerID = e.UserID
                WHERE sr.SRID = {SRID}";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                lblTitle.Text = row["Title"].ToString();
                lblStatus.Text = row["StatusName"].ToString();
                lblRequestor.Text = row["RequestorName"].ToString();
                lblSubmitDate.Text = Convert.ToDateTime(row["SubmitDate"]).ToString("yyyy-MM-dd");

                litPurpose.Text = HttpUtility.HtmlEncode(row["Purpose"].ToString()).Replace("\n", "<br />");
                litScope.Text = HttpUtility.HtmlEncode(row["Scope"].ToString()).Replace("\n", "<br />");
                litBenefit.Text = HttpUtility.HtmlEncode(row["Benefit"].ToString()).Replace("\n", "<br />");

                if (row["EngineerName"] != DBNull.Value)
                    lblAssignedEngineer.Text = row["EngineerName"].ToString();
                if (row["PlannedCompletionDate"] != DBNull.Value)
                    lblPlannedCompletionDate.Text = Convert.ToDateTime(row["PlannedCompletionDate"]).ToString("yyyy-MM-dd");

                BindFiles(rptInitialDocs, row["InitialDocPath"].ToString());
                ShowActionPanels(row["StatusName"].ToString(), Convert.ToInt32(row["RequestorUserID"]), Convert.ToInt32(row["AssignedEngineerID"] == DBNull.Value ? 0 : row["AssignedEngineerID"]));
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

        private void ShowActionPanels(string status, int requestorId, int engineerId)
        {
            if (CurrentUserRole == "Supervisor_L2" && status == "待二階主管審核") pnlSupervisorL2Action.Visible = true;
            if (status == "待會簽審核" && IsCurrentUserAnApprover()) pnlApproverAction.Visible = true;
            if (CurrentUserRole == "Supervisor_L1" && status == "待一階主管指派")
            {
                pnlSupervisorL1Action.Visible = true;
                BindEngineers();
            }
            if (CurrentUserID == engineerId)
            {
                pnlEngineerAction.Visible = true;
                btnAcceptSR.Visible = (status == "待工程師確認");
                btnCompleteDev.Visible = (status == "開發中");
                btnConfirmClosure.Visible = (status == "待工程師確認結單");
            }
            if (CurrentUserID == requestorId && status == "待User上傳結案報告") pnlUserAction.Visible = true;
        }

        private bool IsCurrentUserAnApprover()
        {
            string query = $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApproverUserID = {CurrentUserID} AND ApproverType = 'To' AND ApprovalStatus = '待簽核'";
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", query) > 0;
        }

        private void LoadApprovers()
        {
            string query = $@"
                SELECT u.Username, u.EmployeeID, sra.ApproverType, sra.ApprovalStatus, sra.ApprovalDate, sra.Comments
                FROM ASE_BPCIM_SR_Approvers_HIS sra
                JOIN ASE_BPCIM_SR_Users_DEFINE u ON sra.ApproverUserID = u.UserID
                WHERE sra.SRID = {SRID} ORDER BY sra.ApproverType DESC";
            gvApprovers.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvApprovers.DataBind();
        }

        private void LoadHistory()
        {
            string query = $@"
                SELECT h.ActionDate, u.Username, h.Action, h.Notes
                FROM ASE_BPCIM_SR_Action_HIS h
                JOIN ASE_BPCIM_SR_Users_DEFINE u ON h.ActionByUserID = u.UserID
                WHERE h.SRID = {SRID} ORDER BY h.ActionDate ASC";
            gvHistory.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            gvHistory.DataBind();
        }

        private void BindEngineers()
        {
            string query = "SELECT u.UserID, u.Username FROM ASE_BPCIM_SR_Users_DEFINE u JOIN ASE_BPCIM_SR_Roles_DEFINE r ON u.RoleID = r.RoleID WHERE r.RoleName = 'Engineer' AND u.IsActive = 1";
            ddlEngineers.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            ddlEngineers.DataBind();
            ddlEngineers.Items.Insert(0, new ListItem("-- 請選擇 --", "0"));
        }

        private void BindFiles(Repeater repeater, string fileData)
        {
            if (!string.IsNullOrEmpty(fileData))
            {
                repeater.DataSource = fileData.Split(';').Select(f => new { FileName = f }).ToList();
                repeater.DataBind();
            }
        }

        protected string GetDownloadUrl(string fileName)
        {
            return Page.ResolveUrl(ConfigurationManager.AppSettings["FileUploadPath"].Replace("~", "") + fileName);
        }

        protected string GetStatusBadgeClass(string status)
        {
            switch (status)
            {
                case "已同意": return "bg-success";
                case "已拒絕": return "bg-danger";
                case "待簽核": return "bg-warning text-dark";
                case "知會": return "bg-info text-dark";
                default: return "bg-secondary";
            }
        }

        protected void btnL2Approve_Click(object sender, EventArgs e)
        {
            string nextStatus = HasToApprovers() ? "待會簽審核" : "待一階主管指派";
            int nextStatusId = GetStatusId(nextStatus);
            UpdateSRStatus(nextStatusId, "二階主管同意", "二階主管已同意此 SR。");

            sqlConnect.Insert_Table_DATA("DefaultConnection",
                $"UPDATE ASE_BPCIM_SR_HIS SET SupervisorL2ApprovalStatus = 'Approved', SupervisorL2ApprovalDate = GETDATE() WHERE SRID = {SRID}");

            NotificationHelper.SendNotificationForStatusChange(SRID, nextStatus);
            ReloadPage();
        }

        protected void btnL2Reject_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRejectionReason.Text))
            {
                ShowMessage("拒絕 SR 必須填寫原因。", "warning");
                return;
            }
            string reason = txtRejectionReason.Text.Replace("'", "''");
            UpdateSRStatus(GetStatusId("已拒絕"), "二階主管拒絕", "拒絕原因: " + reason);

            sqlConnect.Insert_Table_DATA("DefaultConnection",
                $"UPDATE ASE_BPCIM_SR_HIS SET SupervisorL2ApprovalStatus = 'Rejected', SupervisorL2ApprovalDate = GETDATE(), RejectionReason = N'{reason}' WHERE SRID = {SRID}");

            NotificationHelper.SendNotificationForStatusChange(SRID, "已拒絕");
            ReloadPage();
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            UpdateApproverStatus("已同意", txtApproverComments.Text);
            if (AreAllToApproversDone())
            {
                UpdateSRStatus(GetStatusId("待一階主管指派"), "會簽完成", "所有 'To' 類型會簽人員已同意。");
                NotificationHelper.SendNotificationForStatusChange(SRID, "待一階主管指派");
            }
            ReloadPage();
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            UpdateApproverStatus("已拒絕", txtApproverComments.Text);
            UpdateSRStatus(GetStatusId("會簽被拒絕"), "會簽被拒絕", $"會簽人員 {Session["Username"]} 已拒絕。");
            NotificationHelper.SendNotificationForStatusChange(SRID, "會簽被拒絕");
            ReloadPage();
        }

        protected void btnAssign_Click(object sender, EventArgs e)
        {
            if (ddlEngineers.SelectedValue == "0")
            {
                ShowMessage("請選擇一位工程師。", "warning");
                return;
            }
            string plannedDateValue = string.IsNullOrEmpty(txtPlannedDate.Text) ? "NULL" : $"'{txtPlannedDate.Text}'";
            UpdateSRStatus(GetStatusId("待工程師確認"), "指派工程師", $"指派給工程師: {ddlEngineers.SelectedItem.Text}");

            sqlConnect.Insert_Table_DATA("DefaultConnection",
                $@"UPDATE ASE_BPCIM_SR_HIS SET AssignedEngineerID = {ddlEngineers.SelectedValue}, AssignmentDate = GETDATE(), PlannedCompletionDate = {plannedDateValue} WHERE SRID = {SRID}");

            NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師確認");
            ReloadPage();
        }

        protected void btnAcceptSR_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("開發中"), "工程師接單", "工程師已確認接單。");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerAcceptanceDate = GETDATE() WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "開發中");
            ReloadPage();
        }

        protected void btnCompleteDev_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("待User上傳結案報告"), "完成開發", "工程師已完成開發。");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerCompletionDate = GETDATE() WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待User上傳結案報告");
            ReloadPage();
        }

        protected void btnUploadClosureReport_Click(object sender, EventArgs e)
        {
            if (!fileUploadClosureReport.HasFiles)
            {
                ShowMessage("請選擇要上傳的結案報告。", "warning");
                return;
            }
            string filePaths = UploadFiles(fileUploadClosureReport);
            string sanitizedFilePaths = filePaths.Replace("'", "''");
            UpdateSRStatus(GetStatusId("待工程師確認結單"), "上傳結案報告", "使用者已上傳結案報告。");

            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET ClosureReportPath = N'{sanitizedFilePaths}' WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師確認結單");
            ReloadPage();
        }

        protected void btnConfirmClosure_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("已結案"), "工程師確認結單", "工程師已確認結案報告，此 SR 已結案。");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerConfirmClosureDate = GETDATE() WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "已結案");
            ReloadPage();
        }

        private void UpdateSRStatus(int newStatusId, string action, string notes)
        {
            int oldStatusId = GetCurrentStatusId();
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET CurrentStatusID = {newStatusId} WHERE SRID = {SRID}");
            AddHistory(action, notes, oldStatusId, newStatusId);
        }

        private void UpdateApproverStatus(string status, string comments)
        {
            string sanitizedComments = comments.Replace("'", "''");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"UPDATE ASE_BPCIM_SR_Approvers_HIS SET ApprovalStatus = N'{status}', Comments = N'{sanitizedComments}', ApprovalDate = GETDATE() WHERE SRID = {SRID} AND ApproverUserID = {CurrentUserID}");
            AddHistory("會簽人員簽核", $"簽核狀態: {status}。意見: {sanitizedComments}");
        }

        private bool HasToApprovers()
        {
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApproverType = 'To'") > 0;
        }

        private bool AreAllToApproversDone()
        {
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApproverType = 'To' AND ApprovalStatus = '待簽核'") == 0;
        }

        private void AddHistory(string action, string notes, int? oldStatusId = null, int? newStatusId = null)
        {
            string sanitizedAction = action.Replace("'", "''");
            string sanitizedNotes = notes.Replace("'", "''");
            string oldStatusValue = oldStatusId.HasValue ? oldStatusId.Value.ToString() : "NULL";
            string newStatusValue = newStatusId.HasValue ? newStatusId.Value.ToString() : "NULL";

            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByUserID, Notes, OldStatusID, NewStatusID) VALUES ({SRID}, N'{sanitizedAction}', {CurrentUserID}, N'{sanitizedNotes}', {oldStatusValue}, {newStatusValue})");
        }

        private int GetCurrentStatusId()
        {
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT CurrentStatusID FROM ASE_BPCIM_SR_HIS WHERE SRID = {SRID}");
        }

        private int GetStatusId(string statusName)
        {
            string sanitizedStatusName = statusName.Replace("'", "''");
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT StatusID FROM ASE_BPCIM_SR_Statuses_DEFINE WHERE StatusName = N'{sanitizedStatusName}'");
        }

        private string UploadFiles(FileUpload fileUploadControl)
        {
            if (fileUploadControl.HasFiles)
            {
                List<string> savedFilePaths = new List<string>();
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (HttpPostedFile postedFile in fileUploadControl.PostedFiles)
                {
                    string fileName = Path.GetFileName(postedFile.FileName);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);
                    postedFile.SaveAs(filePath);
                    savedFilePaths.Add(uniqueFileName);
                }
                return string.Join(";", savedFilePaths);
            }
            return null;
        }

        private void ShowMessage(string message, string type)
        {
            litMessage.Text = $"<div class='alert alert-{type}'>{message}</div>";
        }

        private void ReloadPage()
        {
            Response.Redirect(Request.RawUrl);
        }
    }
}
