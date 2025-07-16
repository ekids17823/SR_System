<%@ Page Title="登入" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SR_System.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="row justify-content-center align-items-center" style="min-height: 70vh;">
        <div class="col-md-6 col-lg-4">
            <div class="card shadow-sm">
                <div class="card-header text-center">
                    <h3>系統登入</h3>
                </div>
                <div class="card-body">
                    <asp:Literal ID="litMessage" runat="server"></asp:Literal>
                    <div class="mb-3">
                        <label for="txtEmployeeID" class="form-label">員工工號</label>
                        <asp:TextBox ID="txtEmployeeID" runat="server" CssClass="form-control"></asp:TextBox>
                    </div>
                    <div class="mb-3">
                        <label for="txtPassword" class="form-label">密碼</label>
                        <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password"></asp:TextBox>
                    </div>
                    <div class="d-grid">
                        <asp:Button ID="btnLogin" runat="server" Text="登入" OnClick="btnLogin_Click" CssClass="btn btn-primary" />
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
