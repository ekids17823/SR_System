<%-- 
================================================================================
檔案：/History.aspx
功能：此頁面用來顯示使用者自己的開單紀錄。
變更：1. GridView 的 SR No. 欄位現在會正確顯示 SR_Number。
      2. 連結的 URL 參數已從 SRID 改為 SR_Number。
================================================================================
--%>
<%@ Page Title="開單紀錄" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="SR_System.History" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="table-responsive">
        <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="False" CssClass="table table-hover table-bordered" DataKeyNames="SRID" EmptyDataText="您尚未提交任何服務請求。">
            <Columns>
                <asp:TemplateField HeaderText="SR No.">
                    <ItemTemplate>
                        <a href='ViewSR.aspx?SR_Number=<%# Eval("SR_Number") %>'><%# Eval("SR_Number") %></a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Title" HeaderText="標題" />
                <asp:BoundField DataField="StatusName" HeaderText="最終狀態" />
                <asp:BoundField DataField="EngineerName" HeaderText="處理人" />
                <asp:BoundField DataField="SubmitDate" HeaderText="開單日期" DataFormatString="{0:yyyy-MM-dd}" />
                <asp:BoundField DataField="EngineerConfirmClosureDate" HeaderText="結案日期" DataFormatString="{0:yyyy-MM-dd}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
