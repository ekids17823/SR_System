// ================================================================================
// 檔案：/CreateSR.aspx.cs
// 功能：處理建立新服務請求 (SR) 的所有後端邏輯。
// 變更：1. 修正了 GetCimGroupForEngineer 方法，使其從黃頁查詢。
//       2. 修正了 btnAddApprover_Click 和 rptApprovers_ItemCommand，使其完全基於 EmployeeID 操作。
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
                BindApproversRepeater();
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (fileUploadInitialDocs.HasFile)
            {
                string originalFileName = Path.GetFileName(fileUploadInitialDocs.PostedFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                fileUploadInitialDocs.SaveAs(filePath);

                hdnFileInfo.Value = $"{uniqueFileName}|{originalFileName}";

                hlUploadedFile.Text = $"已上傳: {originalFileName}";
                hlUploadedFile.NavigateUrl = $"Handlers/FileDownloader.ashx?file={HttpUtility.UrlEncode(uniqueFileName)}";
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

                Response.Redirect($"~/ViewSR.aspx?SRID={newSrId}");
            }
            catch (Exception ex)
            {
                ShowMessage("提交失敗，發生錯誤: " + ex.Message, "danger");
            }
        }

        private int FindOrCreateUserInSystem(string employeeId)
        {
            string sanitizedEmployeeId = employeeId.Replace("'", "''");
            string userQuery = $"SELECT UserID FROM ASE_BPCIM_SR_Users_DEFINE WHERE EmployeeID = N'{sanitizedEmployeeId}'";
            object userIdObj = sqlConnect.Execute_Scalar("DefaultConnection", userQuery);

            if (userIdObj != null)
            {
                return Convert.ToInt32(userIdObj);
            }
            else
            {
                string insertUserQuery = $"INSERT INTO ASE_BPCIM_SR_Users_DEFINE (EmployeeID) OUTPUT INSERTED.UserID VALUES (N'{sanitizedEmployeeId}');";
                return (int)sqlConnect.Execute_Scalar("DefaultConnection", insertUserQuery);
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

        private string UploadFiles()
        {
            if (fileUploadInitialDocs.HasFile)
            {
                string originalFileName = Path.GetFileName(fileUploadInitialDocs.PostedFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                fileUploadInitialDocs.SaveAs(filePath);

                return $"{uniqueFileName}|{originalFileName}";
            }
            return null;
        }

        private void ShowMessage(string message, string type)
        {
            string script = $"alert('{message.Replace("'", "\\'")}');";
            ScriptManager.RegisterStartupScript(this.upApprovers, this.upApprovers.GetType(), "ShowMessage", script, true);
        }
    }
}
