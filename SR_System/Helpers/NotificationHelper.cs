// ================================================================================
// 檔案：/Helpers/NotificationHelper.cs
// 功能：集中處理所有郵件通知的邏輯。
// 變更：1. 全面重構所有查詢，使其完全基於 EmployeeID 並從黃頁資料表獲取資訊。
//       2. 修正了 GetApprovers 和 GetSrDetails 的邏輯。
//       3. 新增了 GetCimBosses 和 GetCimLeader 方法以符合新的審核流程。
// ================================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public static void SendNotificationForStatusChange(int srId, string newStatusName)
        {
            var srDetails = GetSrDetails(srId);
            if (srDetails == null) return;

            string srNumber = srDetails["SR_Number"].ToString();
            string title = srDetails["Title"].ToString();
            string requestorEmployeeId = srDetails["RequestorEmployeeID"].ToString();
            string cimGroup = srDetails["CIM_Group"].ToString();

            string subject = $"[SR-{srNumber}] {newStatusName}: {title}";
            string body = $"<p>您好，</p><p>關於服務請求單 <a href='http://your-server/ViewSR.aspx?SRID={srId}'>SR-{srNumber}</a> 有新的進度更新，目前狀態為：<b>{newStatusName}</b>。</p>";
            List<string> toList = new List<string>();
            List<string> ccList = new List<string> { requestorEmployeeId }; // 開單人預設都會被 CC

            switch (newStatusName)
            {
                case "待開單主管審核":
                    var requesterManager = GetManagerFromYellowPages(requestorEmployeeId);
                    if (requesterManager != null) toList.Add(requesterManager);
                    break;

                case "待會簽審核":
                    var approvers = GetApprovers(srId);
                    toList.AddRange(approvers.Select(a => a.EmployeeID));
                    ccList.AddRange(approvers.Select(a => a.ManagerEmployeeID).Where(m => !string.IsNullOrEmpty(m)));
                    break;

                case "待CIM主管審核":
                    toList.AddRange(GetCimBosses(cimGroup));
                    break;

                case "待CIM主任指派":
                    toList.Add(GetCimLeader(cimGroup));
                    break;

                    // 其他狀態的通知可以陸續加入...
            }

            if (toList.Any())
            {
                SendEmail(subject, body, toList.Distinct().ToArray(), ccList.Distinct().ToArray());
            }
        }

        private static Dictionary<string, object> GetSrDetails(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $"SELECT SR_Number, Title, RequestorEmployeeID, CIM_Group FROM ASE_BPCIM_SR_HIS WHERE SRID = {srId}";
            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return new Dictionary<string, object>
                {
                    {"SR_Number", row["SR_Number"]},
                    {"Title", row["Title"]},
                    {"RequestorEmployeeID", row["RequestorEmployeeID"]},
                    {"CIM_Group", row["CIM_Group"]}
                };
            }
            return null;
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

        private class User
        {
            public string EmployeeID { get; set; }
            public string ManagerEmployeeID { get; set; }
        }
    }
}
