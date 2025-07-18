// ================================================================================
// 檔案：/Handlers/FileDownloader.ashx.cs
// 功能：專門處理檔案下載請求的處理常式。
// 變更：新增了對 'type' 參數的判斷，使其能根據檔案類型從 Web.config 讀取正確的路徑。
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
            if (context.Session["UserID"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Write("Unauthorized");
                context.Response.End();
                return;
            }

            string requestedFile = context.Request.QueryString["file"];
            string fileType = context.Request.QueryString["type"];

            if (string.IsNullOrEmpty(requestedFile) || requestedFile.Contains("..") || requestedFile.Contains("/") || requestedFile.Contains("\\"))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Bad Request");
                context.Response.End();
                return;
            }

            string uploadFolder;
            if (fileType == "closure")
            {
                uploadFolder = context.Server.MapPath(ConfigurationManager.AppSettings["ClosureReportUploadPath"]);
            }
            else // 預設為 initial
            {
                uploadFolder = context.Server.MapPath(ConfigurationManager.AppSettings["InitialDocUploadPath"]);
            }

            string filePath = Path.Combine(uploadFolder, requestedFile);

            if (File.Exists(filePath))
            {
                // 從儲存的檔名中解析出原始檔名
                string originalFileName = requestedFile;
                if (requestedFile.Contains("_"))
                {
                    originalFileName = requestedFile.Substring(requestedFile.IndexOf('_') + 1);
                }

                context.Response.ContentType = "application/octet-stream";
                context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(originalFileName));
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
