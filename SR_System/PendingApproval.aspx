<%@ Page Title="待簽核的 SR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PendingApproval.aspx.cs" Inherits="SR_System.PendingApproval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="table-responsive">
        <asp:GridView ID="gvPendingApproval" runat="server" AutoGenerateColumns="False" CssClass="table table-hover table-bordered" DataKeyNames="SRID" EmptyDataText="目前沒有等待您簽核的 SR。">
            <Columns>
                <asp:TemplateField HeaderText="SR No.">
                    <ItemTemplate>
                        <a href='ViewSR.aspx?SRID=<%# Eval("SRID") %>'><%# Eval("SRID") %></a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Title" HeaderText="標題" />
                <asp:BoundField DataField="Purpose" HeaderText="目的 (Description)" />
                <asp:BoundField DataField="StatusName" HeaderText="目前狀態 (Stage/Status)" />
                <asp:BoundField DataField="RequestorName" HeaderText="開單人" />
                <asp:BoundField DataField="EngineerName" HeaderText="處理人" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
