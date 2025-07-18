// ================================================================================
// 檔案：/Helpers/NotificationHelper.cs
// 功能：集中處理所有郵件通知的邏輯。
// 變更：1. 根據您指定的範本，重構了郵件內容的產生方式。
//       2. 補齊了所有流程節點的郵件通知邏輯。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using SR_System.DAL;

namespace SR_System.Helpers
{
    public static class NotificationHelper
    {
        /// <summary>
        /// 派送郵件的空殼方法。您可以在這裡填入您自己的郵件派送邏輯。
        /// </summary>
        public static void SendEmail(string subject, string body, string[] to, string[] cc)
        {
            System.Diagnostics.Debug.WriteLine("--- Sending Email ---");
            System.Diagnostics.Debug.WriteLine($"Subject: {subject}");
            System.Diagnostics.Debug.WriteLine($"To: {string.Join(", ", to)}");
            System.Diagnostics.Debug.WriteLine($"CC: {string.Join(", ", cc)}");
            System.Diagnostics.Debug.WriteLine("Body:");
            System.Diagnostics.Debug.WriteLine(body);
            System.Diagnostics.Debug.WriteLine("--- Email Sent ---");
        }

        /// <summary>
        /// 根據 SR 狀態的變更，發送對應的通知郵件。
        /// </summary>
        public static void SendNotificationForStatusChange(int srId, string newStatusName, string comment = "")
        {
            var srDetails = GetSrDetails(srId);
            if (srDetails == null) return;

            List<string> toList = new List<string>();
            List<string> ccList = new List<string> { srDetails.RequestorEmployeeID };
            string lastAction = GetLastAction(srId);

            switch (newStatusName)
            {
                case "待開單主管審核":
                    var requesterManager = GetManagerFromYellowPages(srDetails.RequestorEmployeeID);
                    if (requesterManager != null) toList.Add(requesterManager);
                    break;

                case "待會簽審核":
                    var approvers = GetApprovers(srId);
                    toList.AddRange(approvers.Select(a => a.EmployeeID));
                    ccList.AddRange(approvers.Select(a => a.ManagerEmployeeID).Where(m => !string.IsNullOrEmpty(m)));
                    break;

                case "待CIM主管審核":
                    toList.AddRange(GetCimBosses(srDetails.CimGroup));
                    break;

                case "待CIM主任指派":
                    toList.Add(GetCimLeader(srDetails.CimGroup));
                    break;

                case "待工程師接單":
                case "開發中":
                case "待使用者測試":
                case "待使用者上傳報告":
                case "待程式上線":
                case "待工程師結單":
                    if (!string.IsNullOrEmpty(srDetails.AssignedEngineerEmployeeID))
                        toList.Add(srDetails.AssignedEngineerEmployeeID);
                    break;

                case "已結案":
                case "已拒絕":
                case "待開單人修改":
                case "已取消":
                    toList.Add(srDetails.RequestorEmployeeID);
                    break;
            }

            if (toList.Any())
            {
                string body = BuildEmailBody(srDetails.SR_Number, srDetails, lastAction, comment);
                string subject = $"[SR-{srDetails.SR_Number}] {newStatusName}: {srDetails.Title}";
                SendEmail(subject, body, toList.Distinct().ToArray(), ccList.Distinct().ToArray());
            }
        }

        private static string BuildEmailBody(string srNumber, SrInfo sr, string lastAction, string comment)
        {
            var body = new StringBuilder();
            body.AppendLine("---------------------Mail Start-------------------");
            body.AppendLine("<br/><br/>");
            body.AppendLine("Dear Sir : ");
            body.AppendLine("<br/><br/>");
            body.AppendLine("有張需求被提出，請審核 ");
            body.AppendLine("<br/><br/>");
            body.AppendLine($"<b>No:</b> {sr.SR_Number}<br/>");
            body.AppendLine($"<b>Title:</b> {HttpUtility.HtmlEncode(sr.Title)}<br/>");
            body.AppendLine($"<b>Purpose:</b> {HttpUtility.HtmlEncode(sr.Purpose)}<br/>");
            body.AppendLine($"<b>Last Action:</b> {HttpUtility.HtmlEncode(lastAction)}<br/>");
            body.AppendLine($"<b>Comment:</b> {HttpUtility.HtmlEncode(comment)}<br/>");
            body.AppendLine("<br/>");
            body.AppendLine($"詳細內容：請用滑鼠左鍵點兩下打開此份檔案 => <a href='http://your-website.com/ViewSR.aspx?SR_Number={srNumber}'>Link</a>");
            body.AppendLine("<br/><br/>");
            body.AppendLine("CIM SR System ");
            body.AppendLine("<br/><br/>");
            body.AppendLine("-----------------------Mail End------------------------");
            return body.ToString();
        }

        private static SrInfo GetSrDetails(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT SR_Number, Title, Purpose, RequestorEmployeeID, CIM_Group, AssignedEngineerEmployeeID FROM ASE_BPCIM_SR_HIS WHERE SRID = {srId}";
            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return new SrInfo
                {
                    SR_Number = row["SR_Number"].ToString(),
                    Title = row["Title"].ToString(),
                    Purpose = row["Purpose"].ToString(),
                    RequestorEmployeeID = row["RequestorEmployeeID"].ToString(),
                    CimGroup = row["CIM_Group"].ToString(),
                    AssignedEngineerEmployeeID = row["AssignedEngineerEmployeeID"]?.ToString()
                };
            }
            return null;
        }

        private static string GetLastAction(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT TOP 1 Action FROM ASE_BPCIM_SR_Action_HIS WHERE SRID = {srId} ORDER BY HistoryID DESC";
            return sqlConnect.Execute_Scalar("DefaultConnection", query)?.ToString() ?? "N/A";
        }

        private static List<User> GetApprovers(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $@"
                SELECT sra.ApproverEmployeeID, yp.ManagerEmployeeID 
                FROM ASE_BPCIM_SR_Approvers_HIS sra
                LEFT JOIN ASE_BPCIM_SR_YellowPages_TEST yp ON sra.ApproverEmployeeID = yp.EmployeeID
                WHERE sra.SRID = {srId}";

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            return dt.AsEnumerable().Select(row => new User
            {
                EmployeeID = row["ApproverEmployeeID"].ToString(),
                ManagerEmployeeID = row["ManagerEmployeeID"]?.ToString()
            }).ToList();
        }

        private static List<string> GetCimBosses(string cimGroup)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT Boss1EmployeeID, Boss2EmployeeID FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = N'{cimGroup.Replace("'", "''")}'";
            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            var bosses = new List<string>();
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["Boss1EmployeeID"] != DBNull.Value) bosses.Add(dt.Rows[0]["Boss1EmployeeID"].ToString());
                if (dt.Rows[0]["Boss2EmployeeID"] != DBNull.Value) bosses.Add(dt.Rows[0]["Boss2EmployeeID"].ToString());
            }
            return bosses;
        }

        private static string GetCimLeader(string cimGroup)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT LeaderEmployeeID FROM ASE_BPCIM_SR_CIMLeaders_DEFINE WHERE CIM_Group = N'{cimGroup.Replace("'", "''")}'";
            return sqlConnect.Execute_Scalar("DefaultConnection", query)?.ToString();
        }

        private static string GetManagerFromYellowPages(string employeeId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT ManagerEmployeeID FROM ASE_BPCIM_SR_YellowPages_TEST WHERE EmployeeID = N'{employeeId.Replace("'", "''")}'";
            return sqlConnect.Execute_Scalar("DefaultConnection", query)?.ToString();
        }

        private class SrInfo
        {
            public string SR_Number { get; set; }
            public string Title { get; set; }
            public string Purpose { get; set; }
            public string RequestorEmployeeID { get; set; }
            public string CimGroup { get; set; }
            public string AssignedEngineerEmployeeID { get; set; }
        }

        private class User
        {
            public string EmployeeID { get; set; }
            public string ManagerEmployeeID { get; set; }
        }
    }
}
