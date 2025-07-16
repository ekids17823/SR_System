<%@ Page Title="處理中的 SR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Processing.aspx.cs" Inherits="SR_System.Processing" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="table-responsive">
        <asp:GridView ID="gvProcessing" runat="server" AutoGenerateColumns="False" CssClass="table table-hover table-bordered" DataKeyNames="SRID" EmptyDataText="目前沒有處理中的 SR。">
            <Columns>
                <asp:TemplateField HeaderText="SR No.">
                    <ItemTemplate>
                        <a href='ViewSR.aspx?SRID=<%# Eval("SRID") %>'><%# Eval("SRID") %></a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Title" HeaderText="標題" />
                <asp:BoundField DataField="StatusName" HeaderText="目前狀態" />
                <asp:BoundField DataField="RequestorName" HeaderText="開單人" />
                <asp:BoundField DataField="EngineerName" HeaderText="處理人" />
                <asp:BoundField DataField="AssignmentDate" HeaderText="指派日期" DataFormatString="{0:yyyy-MM-dd}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
