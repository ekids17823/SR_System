// ================================================================================
// 檔案：/FileDownloader.ashx.cs (新增)
// 說明：專門處理檔案下載請求的處理常式。
// ================================================================================
using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.SessionState;

namespace SR_System
{
    public class FileDownloader : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            // 安全性檢查：確保使用者已登入
            if (context.Session["UserID"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Write("Unauthorized");
                context.Response.End();
                return;
            }

            string requestedFile = context.Request.QueryString["file"];

            // 安全性檢查：防止路徑遍歷攻擊
            if (string.IsNullOrEmpty(requestedFile) || requestedFile.Contains("..") || requestedFile.Contains("/") || requestedFile.Contains("\\"))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Bad Request");
                context.Response.End();
                return;
            }

            string uploadFolder = context.Server.MapPath(ConfigurationManager.AppSettings["FileUploadPath"]);
            string filePath = Path.Combine(uploadFolder, requestedFile);

            if (File.Exists(filePath))
            {
                context.Response.ContentType = "application/octet-stream";
                // 設定 Content-Disposition 標頭，強制瀏覽器下載檔案
                context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(requestedFile));
                context.Response.TransmitFile(filePath);
                context.Response.End();
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Write("File Not Found");
                context.Response.End();
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
