// ================================================================================
// 檔案：/ViewSR.aspx.cs
// 功能：顯示單一 SR 的詳細資訊，並根據使用者角色和 SR 狀態提供對應的操作。
// 變更：提供了完整的程式碼，移除了所有 #region 區塊。
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
        private int _srId = 0;

        private int SRID
        {
            get
            {
                if (_srId == 0 && !string.IsNullOrEmpty(Request.QueryString["SR_Number"]))
                {
                    _srId = GetSrIdFromSrNumber(Request.QueryString["SR_Number"]);
                }
                return _srId;
            }
        }
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
            if (SRID == 0)
            {
                ShowMessage("找不到指定的 SR 單據。", "danger");
                pnlMainContent.Visible = false;
                return;
            }

            string query = $@"
                WITH NextApprover AS (
                    SELECT 
                        SRID, 
                        ApproverEmployeeID,
                        ROW_NUMBER() OVER(PARTITION BY SRID ORDER BY SRAID) AS rn
                    FROM ASE_BPCIM_SR_Approvers_HIS
                    WHERE ApprovalStatus = N'待簽核'
                ),
                SR_Details AS (
                    SELECT 
                        sr.*, 
                        s.StatusName, 
                        req_yp.Username AS RequestorName, 
                        eng_yp.Username AS EngineerName,
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
                    WHERE sr.SRID = {SRID}
                )
                SELECT * FROM SR_Details";

            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                lblSrNumber.Text = row["SR_Number"].ToString();
                lblTitle.Text = row["Title"].ToString();

                string statusName = row["StatusName"].ToString();
                string currentHandler = row["CurrentHandler"] != DBNull.Value ? row["CurrentHandler"].ToString() : "";
                lblStatus.Text = string.IsNullOrEmpty(currentHandler) || currentHandler == "N/A" ? statusName : $"{statusName} ({currentHandler})";

                lblRequestor.Text = $"{row["RequestorName"]} ({row["RequestorEmployeeID"]})";
                lblSubmitDate.Text = Convert.ToDateTime(row["SubmitDate"]).ToString("yyyy-MM-dd");

                litPurpose.Text = HttpUtility.HtmlEncode(row["Purpose"].ToString()).Replace("\n", "<br />");
                litScope.Text = HttpUtility.HtmlEncode(row["Scope"].ToString()).Replace("\n", "<br />");
                litBenefit.Text = HttpUtility.HtmlEncode(row["Benefit"].ToString()).Replace("\n", "<br />");

                if (row["EngineerName"] != DBNull.Value)
                    lblAssignedEngineer.Text = row["EngineerName"].ToString();
                if (row["PlannedCompletionDate"] != DBNull.Value)
                    lblPlannedCompletionDate.Text = Convert.ToDateTime(row["PlannedCompletionDate"]).ToString("yyyy-MM-dd");

                if (row["EngineerAcceptanceDate"] != DBNull.Value)
                {
                    pnlAcceptanceDate.Visible = true;
                    lblAcceptanceDate.Text = Convert.ToDateTime(row["EngineerAcceptanceDate"]).ToString("yyyy-MM-dd HH:mm");
                }

                if (row["EngineerConfirmClosureDate"] != DBNull.Value)
                {
                    pnlClosureDate.Visible = true;
                    lblClosureDate.Text = Convert.ToDateTime(row["EngineerConfirmClosureDate"]).ToString("yyyy-MM-dd");
                }

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

            pnlActions.Visible = false;
            pnlRequesterManagerAction.Visible = false;
            pnlSignOffAction.Visible = false;
            pnlCimBossAction.Visible = false;
            pnlCimLeaderAction.Visible = false;
            pnlEngineerAction.Visible = false;
            pnlUserAction.Visible = false;
            pnlRequesterEditAction.Visible = false;

            switch (status)
            {
                case "待開單主管審核": if (IsCurrentUserRequesterManager(requestorEmployeeId)) { pnlRequesterManagerAction.Visible = true; pnlActions.Visible = true; } break;
                case "待會簽審核": if (IsCurrentUserASignOffApprover()) { pnlSignOffAction.Visible = true; pnlActions.Visible = true; } break;
                case "待CIM主管審核": if (IsCurrentUserCimBoss(cimGroup)) { pnlCimBossAction.Visible = true; pnlActions.Visible = true; } break;
                case "待CIM主任指派": if (IsCurrentUserCimLeader(cimGroup)) { pnlCimLeaderAction.Visible = true; pnlActions.Visible = true; BindEngineers(cimGroup, engineerEmployeeId); } break;
                case "待工程師接單":
                case "開發中":
                case "待使用者上傳報告":
                case "待程式上線":
                case "待工程師結單":
                    if (CurrentEmployeeID == engineerEmployeeId)
                    {
                        pnlEngineerAction.Visible = true;
                        pnlActions.Visible = true;
                        btnAcceptSR.Visible = (status == "待工程師接單");
                        btnCompleteDev.Visible = (status == "開發中");
                        btnDeploy.Visible = (status == "待使用者上傳報告");
                        btnConfirmClosure.Visible = (status == "待程式上線");
                    }
                    break;
                case "待使用者測試": if (CurrentEmployeeID == requestorEmployeeId) { pnlUserAction.Visible = true; pnlActions.Visible = true; } break;
                case "待開單人修改": if (CurrentEmployeeID == requestorEmployeeId) { pnlRequesterEditAction.Visible = true; pnlActions.Visible = true; } break;
            }
        }

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

        private void BindEngineers(string cimGroup, string assignedEngineerEmployeeId)
        {
            string sanitizedGroup = cimGroup.Replace("'", "''");
            string query = $"SELECT yp.EmployeeID, yp.Username FROM ASE_BPCIM_SR_YellowPages_TEST yp WHERE yp.Department = 'CIM' AND yp.CIM_Group = N'{sanitizedGroup}' AND yp.Position = N'工程師'";
            ddlEngineers.DataSource = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            ddlEngineers.DataTextField = "Username";
            ddlEngineers.DataValueField = "EmployeeID";
            ddlEngineers.DataBind();
            ddlEngineers.Items.Insert(0, new ListItem("-- 請選擇 --", ""));
            if (!string.IsNullOrEmpty(assignedEngineerEmployeeId))
            {
                ListItem selectedItem = ddlEngineers.Items.FindByValue(assignedEngineerEmployeeId);
                if (selectedItem != null)
                {
                    selectedItem.Selected = true;
                }
            }
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

        protected void btnReqManagerApprove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtActionComments.Text)) { ShowMessage("請填寫簽核意見。", "warning"); return; }
            string nextStatus = HasSignOffApprovers() ? "待會簽審核" : "待CIM主管審核";
            int nextStatusId = GetStatusId(nextStatus);
            UpdateSRStatus(nextStatusId, "開單主管同意", txtActionComments.Text.Trim());
            NotificationHelper.SendNotificationForStatusChange(SRID, nextStatus, txtActionComments.Text.Trim());
            ReloadPage();
        }

        protected void btnSignOffApprove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtActionComments.Text)) { ShowMessage("請填寫簽核意見。", "warning"); return; }
            UpdateSignOffApproverStatus("已同意", txtActionComments.Text.Trim());
            if (AreAllSignOffApproversDone())
            {
                UpdateSRStatus(GetStatusId("待CIM主管審核"), "會簽完成", "所有會簽人員已同意。");
                NotificationHelper.SendNotificationForStatusChange(SRID, "待CIM主管審核");
            }
            ReloadPage();
        }

        protected void btnCimBossApprove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtActionComments.Text)) { ShowMessage("請填寫簽核意見。", "warning"); return; }
            UpdateSRStatus(GetStatusId("待CIM主任指派"), "CIM主管同意", txtActionComments.Text.Trim());
            NotificationHelper.SendNotificationForStatusChange(SRID, "待CIM主任指派");
            ReloadPage();
        }

        protected void btnAssign_Click(object sender, EventArgs e)
        {
            Page.Validate("AssignValidation");
            if (!Page.IsValid) return;

            string plannedDateValue = string.IsNullOrEmpty(txtPlannedDate.Text) ? "NULL" : $"'{txtPlannedDate.Text}'";
            string selectedEngineerEmployeeId = ddlEngineers.SelectedValue;
            string notes = $"指派給工程師: {ddlEngineers.SelectedItem.Text}。意見: {txtActionComments.Text.Trim()}";
            UpdateSRStatus(GetStatusId("待工程師接單"), "CIM主任指派", notes);
            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"UPDATE ASE_BPCIM_SR_HIS SET AssignedEngineerEmployeeID = N'{selectedEngineerEmployeeId}', AssignmentDate = GETDATE(), PlannedCompletionDate = {plannedDateValue} WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師接單");
            ReloadPage();
        }

        protected void btnAcceptSR_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtActionComments.Text)) { ShowMessage("請填寫意見。", "warning"); return; }
            UpdateSRStatus(GetStatusId("開發中"), "工程師接單", txtActionComments.Text.Trim());
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerAcceptanceDate = GETDATE() WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "開發中");
            ReloadPage();
        }

        protected void btnCompleteDev_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("待使用者測試"), "完成開發", "工程師已完成開發，待使用者測試。");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待使用者測試");
            ReloadPage();
        }

        protected void btnUploadClosureReport_Click(object sender, EventArgs e)
        {
            if (!fileUploadClosureReport.HasFile)
            {
                ShowMessage("請上傳測試報告。", "warning");
                return;
            }
            string fileInfo = UploadFiles(fileUploadClosureReport, "ClosureReportUploadPath");
            hdnClosureFileInfo.Value = fileInfo;

            var parts = fileInfo.Split('|');
            hlClosureFile.Text = $"已上傳: {parts[1]}";
            hlClosureFile.NavigateUrl = $"Handlers/FileDownloader.ashx?file={HttpUtility.UrlEncode(parts[0])}&type=closure";
            hlClosureFile.Visible = true;
        }

        protected void btnCompleteTest_Click(object sender, EventArgs e)
        {
            Page.Validate("CompleteTestValidation");
            if (!Page.IsValid) return;

            if (string.IsNullOrEmpty(hdnClosureFileInfo.Value))
            {
                ShowMessage("請先上傳測試報告。", "warning");
                return;
            }
            string sanitizedFilePaths = hdnClosureFileInfo.Value.Replace("'", "''");
            UpdateSRStatus(GetStatusId("待程式上線"), "完成測試", txtActionComments.Text.Trim());
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET ClosureReportPath = N'{sanitizedFilePaths}' WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待程式上線", txtActionComments.Text.Trim());
            ReloadPage();
        }

        protected void btnDeploy_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("待工程師結單"), "程式上線", "工程師已將程式上線。");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待工程師結單");
            ReloadPage();
        }

        protected void btnConfirmClosure_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("已結案"), "工程師結單", "工程師已結單，此 SR 已結案。");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET EngineerConfirmClosureDate = GETDATE() WHERE SRID = {SRID}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "已結案");
            ReloadPage();
        }

        protected void btnAction_Reject(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtActionComments.Text)) { ShowMessage("駁回必須填寫原因。", "warning"); return; }
            string reason = txtActionComments.Text.Replace("'", "''");
            UpdateSRStatus(GetStatusId("待開單人修改"), "需求被駁回", $"操作者: {Session["Username"]}，原因: {reason}");
            NotificationHelper.SendNotificationForStatusChange(SRID, "待開單人修改", reason);
            ReloadPage();
        }

        protected void btnEditSR_Click(object sender, EventArgs e)
        {
            Response.Redirect($"~/CreateSR.aspx?SR_Number={lblSrNumber.Text}");
        }

        protected void btnCancelSR_Click(object sender, EventArgs e)
        {
            UpdateSRStatus(GetStatusId("已取消"), "取消SR", $"開單人 {Session["Username"]} 已取消此 SR。");
            NotificationHelper.SendNotificationForStatusChange(SRID, "已取消");
            ReloadPage();
        }

        private int GetSrIdFromSrNumber(string srNumber)
        {
            string sanitizedSrNumber = srNumber.Replace("'", "''");
            string query = $"SELECT SRID FROM ASE_BPCIM_SR_HIS WHERE SR_Number = N'{sanitizedSrNumber}'";
            object result = sqlConnect.Execute_Scalar("DefaultConnection", query);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void UpdateSRStatus(int newStatusId, string action, string notes)
        {
            int oldStatusId = GetCurrentStatusId();
            sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET CurrentStatusID = {newStatusId} WHERE SRID = {SRID}");
            AddHistory(action, notes, oldStatusId, newStatusId);
        }

        private void UpdateSignOffApproverStatus(string status, string comments)
        {
            string sanitizedComments = comments.Replace("'", "''");
            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"UPDATE ASE_BPCIM_SR_Approvers_HIS SET ApprovalStatus = N'{status}', Comments = N'{sanitizedComments}', ApprovalDate = GETDATE() WHERE SRID = {SRID} AND ApproverEmployeeID = N'{CurrentEmployeeID}'");
            AddHistory("會簽人員簽核", $"簽核狀態: {status}。意見: {sanitizedComments}");
        }

        private bool HasSignOffApprovers()
        {
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID}") > 0;
        }

        private bool AreAllSignOffApproversDone()
        {
            return (int)sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT COUNT(1) FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {SRID} AND ApprovalStatus = '待簽核'") == 0;
        }

        private void AddHistory(string action, string notes, int? oldStatusId = null, int? newStatusId = null)
        {
            string sanitizedAction = action.Replace("'", "''");
            string sanitizedNotes = notes.Replace("'", "''");
            string oldStatusValue = oldStatusId.HasValue ? oldStatusId.Value.ToString() : "NULL";
            string newStatusValue = newStatusId.HasValue ? newStatusId.Value.ToString() : "NULL";
            sqlConnect.Insert_Table_DATA("DefaultConnection", $@"INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByEmployeeID, Notes, OldStatusID, NewStatusID) VALUES ({SRID}, N'{sanitizedAction}', N'{CurrentEmployeeID}', N'{sanitizedNotes}', {oldStatusValue}, {newStatusValue})");
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

        private string UploadFiles(FileUpload fileUploadControl, string configKey)
        {
            if (fileUploadControl.HasFile)
            {
                string originalFileName = Path.GetFileName(fileUploadControl.PostedFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings[configKey]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                fileUploadControl.SaveAs(filePath);
                return $"{uniqueFileName}|{originalFileName}";
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

        protected string GetStatusBadgeClass(string status)
        {
            switch (status)
            {
                case "已同意": return "bg-success";
                case "已拒絕": return "bg-danger";
                case "待簽核": return "bg-warning text-dark";
                default: return "bg-secondary";
            }
        }
    }
}
