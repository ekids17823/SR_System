// ================================================================================
// 檔案：/CreateSR.aspx.cs
// 功能：處理建立或編輯服務請求 (SR) 的所有後端邏輯。
// 變更：1. UploadFiles 方法現在會從 Web.config 讀取正確的初始文件路徑。
//       2. btnUpload_Click 中產生的下載連結現在會附帶 type=initial 參數。
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
    public partial class CreateSR : Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        private List<Dictionary<string, object>> ApproversList
        {
            get { return (List<Dictionary<string, object>>)ViewState["ApproversList"] ?? new List<Dictionary<string, object>>(); }
            set { ViewState["ApproversList"] = value; }
        }

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
                if (!string.IsNullOrEmpty(Request.QueryString["SR_Number"]))
                {
                    string srNumber = Request.QueryString["SR_Number"];
                    LoadSrDataForEditing(srNumber);
                }
            }
        }

        private void LoadSrDataForEditing(string srNumber)
        {
            string sanitizedSrNumber = srNumber.Replace("'", "''");
            string query = $"SELECT * FROM ASE_BPCIM_SR_HIS WHERE SR_Number = N'{sanitizedSrNumber}'";
            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                hdnSrId.Value = row["SRID"].ToString();
                pnlNewSrActions.Visible = false;
                pnlEditSrActions.Visible = true;

                txtTitle.Text = row["Title"].ToString();
                txtPurpose.Text = row["Purpose"].ToString();
                txtScope.Text = row["Scope"].ToString();
                txtBenefit.Text = row["Benefit"].ToString();

                var engineer = GetUserFromYellowPages(row["AssignedEngineerEmployeeID"].ToString());
                if (engineer != null)
                {
                    hdnEngineerEmployeeId.Value = engineer["EmployeeID"].ToString();
                    txtEngineerSearch.Value = $"{engineer["Username"]} ({engineer["EmployeeID"]})";
                }

                string fileInfo = row["InitialDocPath"].ToString();
                if (!string.IsNullOrEmpty(fileInfo))
                {
                    hdnFileInfo.Value = fileInfo;
                    var parts = fileInfo.Split('|');
                    hlUploadedFile.Text = $"已上傳: {parts[1]}";
                    hlUploadedFile.NavigateUrl = $"Handlers/FileDownloader.ashx?file={HttpUtility.UrlEncode(parts[0])}&type=initial";
                    hlUploadedFile.Visible = true;
                }

                LoadApproversForEditing(Convert.ToInt32(row["SRID"]));
            }
        }

        private void LoadApproversForEditing(int srId)
        {
            string query = $"SELECT ApproverEmployeeID FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {srId}";
            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            var currentList = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var user = GetUserFromYellowPages(row["ApproverEmployeeID"].ToString());
                if (user != null)
                {
                    currentList.Add(user);
                }
            }
            this.ApproversList = currentList;
            BindApproversRepeater();
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (fileUploadInitialDocs.HasFile)
            {
                string originalFileName = Path.GetFileName(fileUploadInitialDocs.PostedFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["InitialDocUploadPath"]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                fileUploadInitialDocs.SaveAs(filePath);

                hdnFileInfo.Value = $"{uniqueFileName}|{originalFileName}";

                hlUploadedFile.Text = $"已上傳: {originalFileName}";
                hlUploadedFile.NavigateUrl = $"Handlers/FileDownloader.ashx?file={HttpUtility.UrlEncode(uniqueFileName)}&type=initial";
                hlUploadedFile.Visible = true;
            }
            else
            {
                ShowMessage("請選擇一個檔案進行上傳。", "warning");
            }
        }

        protected void btnAddApprover_Click(object sender, EventArgs e)
        {
            string approverEmployeeId = hdnApproverEmployeeId.Value;

            if (!string.IsNullOrEmpty(approverEmployeeId))
            {
                if (ApproversList.Any(a => a["EmployeeID"].ToString().Equals(approverEmployeeId, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowMessage("此人員已在會簽列表中。", "warning");
                    return;
                }

                var user = GetUserFromYellowPages(approverEmployeeId);
                if (user != null)
                {
                    var currentList = this.ApproversList;
                    currentList.Add(user);
                    this.ApproversList = currentList;
                    BindApproversRepeater();

                    hdnApproverEmployeeId.Value = "";
                    txtApproverSearch.Value = "";
                }
                else
                {
                    ShowMessage("在黃頁中找不到此員工。", "danger");
                }
            }
            else
            {
                ShowMessage("請從搜尋結果中選擇一位有效的會簽人員。", "danger");
            }
        }

        protected void rptApprovers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Remove")
            {
                string employeeIdToRemove = e.CommandArgument.ToString();
                var currentList = this.ApproversList;
                var itemToRemove = currentList.FirstOrDefault(a => a["EmployeeID"].ToString().Equals(employeeIdToRemove, StringComparison.OrdinalIgnoreCase));
                if (itemToRemove != null)
                {
                    currentList.Remove(itemToRemove);
                    this.ApproversList = currentList;
                    BindApproversRepeater();
                }
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (hdnSrId.Value == "0")
            {
                CreateNewSR();
            }
            else
            {
                UpdateExistingSR(Convert.ToInt32(hdnSrId.Value));
            }
        }

        private void CreateNewSR()
        {
            string engineerEmployeeId = hdnEngineerEmployeeId.Value;
            if (string.IsNullOrEmpty(engineerEmployeeId))
            {
                ShowMessage("提交失敗：請選擇一位有效的CIM工程師。", "danger");
                return;
            }

            string requestorEmployeeId = Session["EmployeeID"].ToString();
            string fileInfo = hdnFileInfo.Value;

            string srNumber = GenerateSrNumber();
            string cimGroup = GetCimGroupForEngineer(engineerEmployeeId);

            if (string.IsNullOrEmpty(cimGroup))
            {
                ShowMessage("錯誤：找不到所選工程師對應的CIM組別。", "danger");
                return;
            }

            try
            {
                string sanitizedTitle = txtTitle.Text.Trim().Replace("'", "''");
                string sanitizedPurpose = txtPurpose.Text.Trim().Replace("'", "''");
                string sanitizedScope = txtScope.Text.Trim().Replace("'", "''");
                string sanitizedBenefit = txtBenefit.Text.Trim().Replace("'", "''");
                string sanitizedFilePaths = (fileInfo ?? "").Replace("'", "''");
                int statusId = GetStatusId("待開單主管審核");

                string srQuery = $@"
                    INSERT INTO ASE_BPCIM_SR_HIS (SR_Number, CIM_Group, Title, RequestorEmployeeID, Purpose, Scope, Benefit, InitialDocPath, CurrentStatusID, SubmitDate, AssignedEngineerEmployeeID)
                    OUTPUT INSERTED.SRID
                    VALUES (N'{srNumber}', N'{cimGroup}', N'{sanitizedTitle}', N'{requestorEmployeeId}', N'{sanitizedPurpose}', N'{sanitizedScope}', N'{sanitizedBenefit}', N'{sanitizedFilePaths}', {statusId}, GETDATE(), N'{engineerEmployeeId}');";

                int newSrId = (int)sqlConnect.Execute_Scalar("DefaultConnection", srQuery);

                if (ApproversList.Any())
                {
                    foreach (var approver in ApproversList)
                    {
                        string approverEmpId = approver["EmployeeID"].ToString();
                        string approverQuery = $@"
                            INSERT INTO ASE_BPCIM_SR_Approvers_HIS (SRID, ApproverEmployeeID, ApprovalStatus)
                            VALUES ({newSrId}, N'{approverEmpId}', N'待簽核');";
                        sqlConnect.Insert_Table_DATA("DefaultConnection", approverQuery);
                    }
                }

                string historyQuery = $@"
                    INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByEmployeeID, NewStatusID, Notes)
                    VALUES ({newSrId}, N'提交SR', N'{requestorEmployeeId}', {statusId}, N'建立新的服務請求');";
                sqlConnect.Insert_Table_DATA("DefaultConnection", historyQuery);

                NotificationHelper.SendNotificationForStatusChange(newSrId, "待開單主管審核");

                Response.Redirect($"~/ViewSR.aspx?SR_Number={srNumber}");
            }
            catch (Exception ex)
            {
                ShowMessage("提交失敗，發生錯誤: " + ex.Message, "danger");
            }
        }

        private void UpdateExistingSR(int srId)
        {
            string engineerEmployeeId = hdnEngineerEmployeeId.Value;
            if (string.IsNullOrEmpty(engineerEmployeeId))
            {
                ShowMessage("提交失敗：請選擇一位有效的CIM工程師。", "danger");
                return;
            }

            string requestorEmployeeId = Session["EmployeeID"].ToString();
            string fileInfo = hdnFileInfo.Value;
            string cimGroup = GetCimGroupForEngineer(engineerEmployeeId);

            if (string.IsNullOrEmpty(cimGroup))
            {
                ShowMessage("錯誤：找不到所選工程師對應的CIM組別。", "danger");
                return;
            }

            try
            {
                string sanitizedTitle = txtTitle.Text.Trim().Replace("'", "''");
                string sanitizedPurpose = txtPurpose.Text.Trim().Replace("'", "''");
                string sanitizedScope = txtScope.Text.Trim().Replace("'", "''");
                string sanitizedBenefit = txtBenefit.Text.Trim().Replace("'", "''");
                string sanitizedFilePaths = (fileInfo ?? "").Replace("'", "''");
                int statusId = GetStatusId("待開單主管審核");

                string updateQuery = $@"
                    UPDATE ASE_BPCIM_SR_HIS SET
                    Title = N'{sanitizedTitle}',
                    Purpose = N'{sanitizedPurpose}',
                    Scope = N'{sanitizedScope}',
                    Benefit = N'{sanitizedBenefit}',
                    InitialDocPath = N'{sanitizedFilePaths}',
                    CIM_Group = N'{cimGroup}',
                    AssignedEngineerEmployeeID = N'{engineerEmployeeId}',
                    CurrentStatusID = {statusId}
                    WHERE SRID = {srId}";

                sqlConnect.Insert_Table_DATA("DefaultConnection", updateQuery);

                sqlConnect.Insert_Table_DATA("DefaultConnection", $"DELETE FROM ASE_BPCIM_SR_Approvers_HIS WHERE SRID = {srId}");
                if (ApproversList.Any())
                {
                    foreach (var approver in ApproversList)
                    {
                        string approverEmpId = approver["EmployeeID"].ToString();
                        string approverQuery = $@"
                            INSERT INTO ASE_BPCIM_SR_Approvers_HIS (SRID, ApproverEmployeeID, ApprovalStatus)
                            VALUES ({srId}, N'{approverEmpId}', N'待簽核');";
                        sqlConnect.Insert_Table_DATA("DefaultConnection", approverQuery);
                    }
                }

                string historyQuery = $@"
                    INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByEmployeeID, NewStatusID, Notes)
                    VALUES ({srId}, N'重新提交SR', N'{requestorEmployeeId}', {statusId}, N'開單人修改後重新提交');";
                sqlConnect.Insert_Table_DATA("DefaultConnection", historyQuery);

                NotificationHelper.SendNotificationForStatusChange(srId, "待開單主管審核");

                string srNumber = sqlConnect.Execute_Scalar("DefaultConnection", $"SELECT SR_Number FROM ASE_BPCIM_SR_HIS WHERE SRID = {srId}").ToString();
                Response.Redirect($"~/ViewSR.aspx?SR_Number={srNumber}");
            }
            catch (Exception ex)
            {
                ShowMessage("更新失敗，發生錯誤: " + ex.Message, "danger");
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            int srId = Convert.ToInt32(hdnSrId.Value);
            if (srId > 0)
            {
                string actionBy = Session["EmployeeID"].ToString();
                int newStatusId = GetStatusId("已取消");
                sqlConnect.Insert_Table_DATA("DefaultConnection", $"UPDATE ASE_BPCIM_SR_HIS SET CurrentStatusID = {newStatusId} WHERE SRID = {srId}");
                sqlConnect.Insert_Table_DATA("DefaultConnection", $@"INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByEmployeeID, NewStatusID, Notes) VALUES ({srId}, N'取消SR', N'{actionBy}', {newStatusId}, N'開單人取消此服務請求')");
                Response.Redirect("~/Processing.aspx");
            }
        }

        protected void ValidateFileUpload(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrEmpty(hdnFileInfo.Value);
        }

        protected void ValidateEngineerSelection(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrEmpty(hdnEngineerEmployeeId.Value);
        }

        private void BindApproversRepeater()
        {
            rptApprovers.DataSource = ApproversList;
            rptApprovers.DataBind();
            pnlEmptyApprovers.Visible = !ApproversList.Any();
        }

        private string GetCimGroupForEngineer(string engineerEmployeeId)
        {
            string query = $"SELECT CIM_Group FROM ASE_BPCIM_SR_YellowPages_TEST WHERE EmployeeID = N'{engineerEmployeeId.Replace("'", "''")}'";
            return sqlConnect.Execute_Scalar("DefaultConnection", query)?.ToString();
        }

        private string GenerateSrNumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string query = $"SELECT COUNT(1) FROM ASE_BPCIM_SR_HIS WHERE SR_Number LIKE N'{datePart}-%'";
            int count = (int)sqlConnect.Execute_Scalar("DefaultConnection", query);
            return $"{datePart}-{(count + 1).ToString("D3")}";
        }

        private Dictionary<string, object> GetUserFromYellowPages(string employeeId)
        {
            string sanitizedId = employeeId.Replace("'", "''");
            string query = $"SELECT EmployeeID, Username FROM ASE_BPCIM_SR_YellowPages_TEST WHERE EmployeeID = N'{sanitizedId}'";
            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new Dictionary<string, object>
                {
                    { "Username", row["Username"].ToString() },
                    { "EmployeeID", row["EmployeeID"].ToString() }
                };
            }
            return null;
        }

        private int GetStatusId(string statusName)
        {
            string sanitizedStatusName = statusName.Replace("'", "''");
            string query = $"SELECT StatusID FROM ASE_BPCIM_SR_Statuses_DEFINE WHERE StatusName = N'{sanitizedStatusName}'";
            object result = sqlConnect.Execute_Scalar("DefaultConnection", query);
            return result != null ? Convert.ToInt32(result) : -1;
        }

        private void ShowMessage(string message, string type)
        {
            string script = $"alert('{message.Replace("'", "\\'")}');";
            ScriptManager.RegisterStartupScript(this.upApprovers, this.upApprovers.GetType(), "ShowMessage", script, true);
        }
    }
}
