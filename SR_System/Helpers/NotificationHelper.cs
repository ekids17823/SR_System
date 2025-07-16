// ================================================================================
// 檔案：/Helpers/NotificationHelper.cs (新增)
// 說明：集中處理所有郵件通知的邏輯。
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
        /// <param name="subject">郵件主旨</param>
        /// <param name="body">郵件內容 (HTML 格式)</param>
        /// <param name="to">收件人列表 (員工工號陣列)</param>
        /// <param name="cc">副本收件人列表 (員工工號陣列)</param>
        public static void SendEmail(string subject, string body, string[] to, string[] cc)
        {
            // --- 請在此處填入您的郵件派送邏輯 ---

            // 偵錯用：在 Visual Studio 的輸出視窗顯示郵件內容，方便開發時驗證。
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

            string subject = "";
            string body = "";
            List<string> toList = new List<string>();
            List<string> ccList = new List<string>();

            string srLink = $"<a href='http://your-server/ViewSR.aspx?SRID={srId}'>SR-{srId}: {srDetails["Title"]}</a>";
            string baseBody = $"<p>您好，</p><p>關於服務請求單 {srLink} 有新的進度更新。</p>";

            switch (newStatusName)
            {
                case "待二階主管審核":
                    subject = $"[SR-{srId}] 新服務請求待審核: {srDetails["Title"]}";
                    // 從資料庫中找到 L2 主管
                    var l2Supervisor = GetUserByRole("Supervisor_L2");
                    if (l2Supervisor != null) toList.Add(l2Supervisor["EmployeeID"].ToString());
                    body = $"{baseBody}<p>新的服務請求已提交，需要您進行二階審核。</p>";
                    ccList.Add(srDetails["RequestorEmployeeID"].ToString());
                    break;

                case "待會簽審核":
                    subject = $"[SR-{srId}] 服務請求待您會簽: {srDetails["Title"]}";
                    var approvers = GetApprovers(srId);
                    toList.AddRange(approvers.Select(a => a.EmployeeID));
                    ccList.AddRange(approvers.Select(a => a.ManagerEmployeeID).Where(m => !string.IsNullOrEmpty(m)));
                    ccList.Add(srDetails["RequestorEmployeeID"].ToString());
                    body = $"{baseBody}<p>此服務請求已通過高階主管審核，需要您進行會簽。</p>";
                    break;

                    // 其他狀態的通知...
            }

            if (toList.Any())
            {
                SendEmail(subject, body, toList.Distinct().ToArray(), ccList.Distinct().ToArray());
            }
        }

        private static Dictionary<string, object> GetSrDetails(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $@"
                SELECT 
                    sr.Title, 
                    u_req.EmployeeID as RequestorEmployeeID, 
                    u_eng.EmployeeID as EngineerEmployeeID
                FROM ASE_BPCIM_SR_HIS sr
                JOIN ASE_BPCIM_SR_Users_DEFINE u_req ON sr.RequestorUserID = u_req.UserID
                LEFT JOIN ASE_BPCIM_SR_Users_DEFINE u_eng ON sr.AssignedEngineerID = u_eng.UserID
                WHERE sr.SRID = {srId}";

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return new Dictionary<string, object>
                {
                    {"Title", row["Title"]},
                    {"RequestorEmployeeID", row["RequestorEmployeeID"]},
                    {"EngineerEmployeeID", row["EngineerEmployeeID"]}
                };
            }
            return null;
        }

        private static List<User> GetApprovers(int srId)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $@"
                SELECT u.EmployeeID, u.ManagerEmployeeID 
                FROM ASE_BPCIM_SR_Approvers_HIS sra
                JOIN ASE_BPCIM_SR_Users_DEFINE u ON sra.ApproverUserID = u.UserID
                WHERE sra.SRID = {srId} AND sra.ApproverType = 'To'";

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            return dt.AsEnumerable().Select(row => new User
            {
                EmployeeID = row["EmployeeID"].ToString(),
                ManagerEmployeeID = row["ManagerEmployeeID"]?.ToString()
            }).ToList();
        }

        private static Dictionary<string, object> GetUserByRole(string roleName)
        {
            SQLDBEntity sqlConnect = new SQLDBEntity();
            string query = $@"
                SELECT TOP 1 u.EmployeeID 
                FROM ASE_BPCIM_SR_Users_DEFINE u
                JOIN ASE_BPCIM_SR_Roles_DEFINE r ON u.RoleID = r.RoleID
                WHERE r.RoleName = N'{roleName}'";

            var dt = sqlConnect.Get_Table_DATA("DefaultConnection", query);
            if (dt.Rows.Count > 0)
            {
                return new Dictionary<string, object> { { "EmployeeID", dt.Rows[0]["EmployeeID"] } };
            }
            return null;
        }

        private class User
        {
            public string EmployeeID { get; set; }
            public string ManagerEmployeeID { get; set; }
        }
    }
}
