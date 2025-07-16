// ================================================================================
// 檔案：/CreateSR.aspx.cs
// 變更：1. 新增 ValidateFileUpload 方法來處理檔案上傳的伺服器端驗證。
//       2. 確保 btnSubmit_Click 方法會檢查 Page.IsValid。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SR_System.DAL;
using SR_System.Helpers;

namespace SR_System
{
    public partial class CreateSR : Page
    {
        private SQLDBEntity sqlConnect = new SQLDBEntity();

        private List<Approver> ApproversList
        {
            get { return (List<Approver>)ViewState["ApproversList"] ?? new List<Approver>(); }
            set { ViewState["ApproversList"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindApproversRepeater();
            }
        }

        protected void btnAddApprover_Click(object sender, EventArgs e)
        {
            string approverUserIdStr = hdnApproverUserId.Value;

            if (!string.IsNullOrEmpty(approverUserIdStr) && int.TryParse(approverUserIdStr, out int approverUserId))
            {
                if (ApproversList.Any(a => a.UserID == approverUserId))
                {
                    ShowMessage("此人員已在會簽列表中。", "warning");
                    return;
                }

                var user = GetUserById(approverUserId);
                if (user != null)
                {
                    var currentList = this.ApproversList;
                    currentList.Add(new Approver
                    {
                        UserID = user.UserID,
                        Username = user.Username,
                        EmployeeID = user.EmployeeID
                    });
                    this.ApproversList = currentList;
                    BindApproversRepeater();

                    hdnApproverUserId.Value = "";
                    txtApproverSearch.Value = "";
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
                int userIdToRemove = Convert.ToInt32(e.CommandArgument);
                var currentList = this.ApproversList;
                var itemToRemove = currentList.FirstOrDefault(a => a.UserID == userIdToRemove);
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

            int requestorUserId = (int)Session["UserID"];
            string filePaths = UploadFiles();

            string constr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string sanitizedTitle = txtTitle.Text.Trim().Replace("'", "''");
                    string sanitizedPurpose = txtPurpose.Text.Trim().Replace("'", "''");
                    string sanitizedScope = txtScope.Text.Trim().Replace("'", "''");
                    string sanitizedBenefit = txtBenefit.Text.Trim().Replace("'", "''");
                    string sanitizedFilePaths = (filePaths ?? "").Replace("'", "''");
                    int statusId = GetStatusId("待二階主管審核");

                    string srQuery = $@"
                        INSERT INTO ASE_BPCIM_SR_HIS (Title, RequestorUserID, Purpose, Scope, Benefit, InitialDocPath, CurrentStatusID, SubmitDate)
                        OUTPUT INSERTED.SRID
                        VALUES (N'{sanitizedTitle}', {requestorUserId}, N'{sanitizedPurpose}', N'{sanitizedScope}', N'{sanitizedBenefit}', N'{sanitizedFilePaths}', {statusId}, GETDATE());";

                    SqlCommand srCmd = new SqlCommand(srQuery, con, transaction);
                    int newSrId = (int)srCmd.ExecuteScalar();

                    foreach (var approver in ApproversList)
                    {
                        string approverQuery = $@"
                            INSERT INTO ASE_BPCIM_SR_Approvers_HIS (SRID, ApproverUserID, ApproverType, ApprovalStatus)
                            VALUES ({newSrId}, {approver.UserID}, N'To', N'待簽核');";
                        SqlCommand approverCmd = new SqlCommand(approverQuery, con, transaction);
                        approverCmd.ExecuteNonQuery();
                    }

                    string historyQuery = $@"
                        INSERT INTO ASE_BPCIM_SR_Action_HIS (SRID, Action, ActionByUserID, NewStatusID, Notes)
                        VALUES ({newSrId}, N'提交SR', {requestorUserId}, {statusId}, N'建立新的服務請求');";
                    SqlCommand historyCmd = new SqlCommand(historyQuery, con, transaction);
                    historyCmd.ExecuteNonQuery();

                    transaction.Commit();

                    NotificationHelper.SendNotificationForStatusChange(newSrId, "待二階主管審核");

                    Response.Redirect($"~/ViewSR.aspx?SRID={newSrId}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ShowMessage("提交失敗，發生錯誤: " + ex.Message, "danger");
                }
            }
        }

        /// <summary>
        /// 伺服器端驗證方法，用於檢查是否有上傳檔案。
        /// </summary>
        protected void ValidateFileUpload(object source, ServerValidateEventArgs args)
        {
            args.IsValid = fileUploadInitialDocs.HasFiles;
        }

        private void BindApproversRepeater()
        {
            rptApprovers.DataSource = ApproversList;
            rptApprovers.DataBind();
            pnlEmptyApprovers.Visible = !ApproversList.Any();
        }

        private User GetUserById(int userId)
        {
            string query = $"SELECT UserID, Username, EmployeeID FROM ASE_BPCIM_SR_Users_DEFINE WHERE UserID = {userId}";
            DataTable dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new User
                {
                    UserID = (int)row["UserID"],
                    Username = row["Username"].ToString(),
                    EmployeeID = row["EmployeeID"].ToString()
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
            if (fileUploadInitialDocs.HasFiles)
            {
                List<string> savedFilePaths = new List<string>();
                string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (HttpPostedFile postedFile in fileUploadInitialDocs.PostedFiles)
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
            string script = $"alert('{message.Replace("'", "\\'")}');";
            ScriptManager.RegisterStartupScript(this.upApprovers, this.upApprovers.GetType(), "ShowMessage", script, true);
        }

        [Serializable]
        public class Approver
        {
            public int UserID { get; set; }
            public string Username { get; set; }
            public string EmployeeID { get; set; }
        }

        public class User
        {
            public int UserID { get; set; }
            public string Username { get; set; }
            public string EmployeeID { get; set; }
        }
    }
}
