<%-- 
================================================================================
檔案：/EngineerHistory.aspx
功能：此頁面專門用來顯示 CIM 工程師自己的結單歷史紀錄。
變更：新增了 <asp:Literal> 控制項，用於顯示可能的錯誤訊息。
================================================================================
--%>
<%@ Page Title="結單歷史紀錄" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EngineerHistory.aspx.cs" Inherits="SR_System.EngineerHistory" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <asp:Literal ID="litMessage" runat="server"></asp:Literal>
    <asp:Panel ID="pnlContent" runat="server">
        <asp:UpdatePanel ID="upHistory" runat="server">
            <ContentTemplate>
                <div class="card mb-4">
                    <div class="card-header">
                        查詢條件
                    </div>
                    <div class="card-body">
                        <div class="row g-3">
                            <div class="col-md-3">
                                <label for="<%= txtStartDate.ClientID %>" class="form-label">起始日期</label>
                                <asp:TextBox ID="txtStartDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label for="<%= txtEndDate.ClientID %>" class="form-label">結束日期</label>
                                <asp:TextBox ID="txtEndDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                            </div>
                            <div class="col-md-3">
                                <label for="<%= ddlStatus.ClientID %>" class="form-label">單據狀態</label>
                                <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select"></asp:DropDownList>
                            </div>
                             <div class="col-md-3">
                                <label for="<%= ddlCimGroup.ClientID %>" class="form-label">組別</label>
                                <asp:DropDownList ID="ddlCimGroup" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlCimGroup_SelectedIndexChanged"></asp:DropDownList>
                            </div>
                            <div class="col-md-3">
                                <label for="<%= ddlEngineer.ClientID %>" class="form-label">工程師</label>
                                <asp:DropDownList ID="ddlEngineer" runat="server" CssClass="form-select"></asp:DropDownList>
                            </div>
                            <div class="col-md-9 d-flex align-items-end">
                                <asp:Button ID="btnSearch" runat="server" Text="查詢" OnClick="btnSearch_Click" CssClass="btn btn-primary" />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="gvEngineerHistory" runat="server" AutoGenerateColumns="False" CssClass="table table-hover table-bordered" DataKeyNames="SRID" EmptyDataText="查無符合條件的 SR 單據。">
                        <Columns>
                            <asp:TemplateField HeaderText="SR No.">
                                <ItemTemplate>
                                    <a href='ViewSR.aspx?SR_Number=<%# Eval("SR_Number") %>'><%# Eval("SR_Number") %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Title" HeaderText="標題" />
                            <asp:BoundField DataField="RequestorName" HeaderText="開單人" />
                            <asp:BoundField DataField="SubmitDate" HeaderText="開單日期" DataFormatString="{0:yyyy-MM-dd}" />
                            <asp:BoundField DataField="EngineerConfirmClosureDate" HeaderText="結案日期" DataFormatString="{0:yyyy-MM-dd}" />
                        </Columns>
                    </asp:GridView>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </asp:Panel>
</asp:Content>
