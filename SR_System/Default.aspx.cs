// ================================================================================
// 檔案：/Default.aspx.cs
// 變更：移除了所有載入儀表板的邏輯，使其成為一個簡單的頁面。
// ================================================================================
using System;
using System.Web.UI;

namespace SR_System
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // 現在這個頁面只做為歡迎頁，不需要載入任何特定資料。
            // 權限檢查已由 Web.config 和 Site.Master 處理。
        }
    }
}
