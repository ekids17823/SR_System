<%-- 
================================================================================
檔案：/Processing.aspx
功能：顯示所有待處理的 SR 列表。
變更：連結的 URL 參數已從 SRID 改為 SR_Number。
================================================================================
--%>
<%@ Page Title="處理中的 SR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Processing.aspx.cs" Inherits="SR_System.Processing" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="table-responsive">
        <asp:GridView ID="gvProcessing" runat="server" AutoGenerateColumns="False" CssClass="table table-hover table-bordered" 
            DataKeyNames="SRID" EmptyDataText="目前沒有待處理的 SR。" OnRowDataBound="gvProcessing_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="SR No.">
                    <ItemTemplate>
                        <asp:HyperLink ID="hlSRLink" runat="server" Text='<%# Eval("SR_Number") %>'></asp:HyperLink>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Title" HeaderText="標題" />
                <asp:BoundField DataField="StatusName" HeaderText="目前狀態" />
                <asp:BoundField DataField="RequestorName" HeaderText="開單人" />
                <asp:BoundField DataField="CurrentHandler" HeaderText="目前處理人" />
                <asp:BoundField DataField="SubmitDate" HeaderText="開單日期" DataFormatString="{0:yyyy-MM-dd}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
