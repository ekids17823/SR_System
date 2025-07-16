<%@ Page Title="首頁" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SR_System._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="PrimaryContent" runat="server">
    <div class="p-5 mb-4 bg-light rounded-3">
        <div class="container-fluid py-5">
            <h1 class="display-5 fw-bold">歡迎使用 SR 管理系統</h1>
            <p class="col-md-8 fs-4">請使用左側的導覽列開始操作，或點擊下方按鈕快速建立新的服務請求。</p>
            <a class="btn btn-primary btn-lg" href="CreateSR.aspx" role="button">建立新 SR</a>
        </div>
    </div>
</asp:Content>
