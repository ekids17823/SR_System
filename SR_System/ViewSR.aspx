<%-- 
================================================================================
檔案：/ViewSR.aspx
功能：顯示單一 SR 的詳細資訊，並根據使用者角色和 SR 狀態提供對應的操作。
變更：1. 修正了附件 Repeater 的下載連結。
      2. 新增了一個共用的「駁回/簽核意見」文字框。
      3. 將結案報告上傳改為單檔模式。
================================================================================
--%>
<%@ Page Title="查看 SR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ViewSR.aspx.cs" Inherits="SR_System.ViewSR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <asp:Literal ID="litMessage" runat="server"></asp:Literal>
    
    <asp:Panel ID="pnlMainContent" runat="server">
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h3>SR 單號: <asp:Label ID="lblSrNumber" runat="server"></asp:Label></h3>
            <h4><span class="badge bg-success"><asp:Label ID="lblStatus" runat="server"></asp:Label></span></h4>
        </div>
        <h5>標題: <asp:Label ID="lblTitle" runat="server"></asp:Label></h5>
        <hr />

        <div class="card mb-4">
            <div class="card-header">SR 詳細資訊</div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>提交人:</strong> <asp:Label ID="lblRequestor" runat="server"></asp:Label></p>
                        <p><strong>提交日期:</strong> <asp:Label ID="lblSubmitDate" runat="server"></asp:Label></p>
                    </div>
                    <div class="col-md-6">
                        <p><strong>指派的工程師:</strong> <asp:Label ID="lblAssignedEngineer" runat="server">N/A</asp:Label></p>
                        <p><strong>預計完成日期:</strong> <asp:Label ID="lblPlannedCompletionDate" runat="server">N/A</asp:Label></p>
                    </div>
                </div>
                <h5>目的</h5>
                <p><asp:Literal ID="litPurpose" runat="server"></asp:Literal></p>
                <h5>範圍</h5>
                <p><asp:Literal ID="litScope" runat="server"></asp:Literal></p>
                <h5>效益</h5>
                <p><asp:Literal ID="litBenefit" runat="server"></asp:Literal></p>
                <h5>附件</h5>
                <asp:Repeater ID="rptInitialDocs" runat="server">
                    <ItemTemplate>
                        <a href='Handlers/FileDownloader.ashx?file=<%# HttpUtility.UrlEncode(Eval("UniqueFileName").ToString()) %>' class="me-3"><%# Eval("OriginalFileName") %></a>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>

        <asp:Panel ID="pnlApproverList" runat="server" Visible="false" class="card mb-4">
            <div class="card-header">會簽進度</div>
            <div class="card-body">
                <asp:GridView ID="gvApprovers" runat="server" AutoGenerateColumns="False" CssClass="table table-sm">
                    <Columns>
                        <asp:BoundField DataField="Username" HeaderText="姓名" />
                        <asp:BoundField DataField="EmployeeID" HeaderText="工號" />
                        <asp:TemplateField HeaderText="狀態">
                             <ItemTemplate>
                                <span class='badge <%# GetStatusBadgeClass(Eval("ApprovalStatus").ToString()) %>'><%# Eval("ApprovalStatus") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="ApprovalDate" HeaderText="簽核日期" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                        <asp:BoundField DataField="Comments" HeaderText="簽核意見" />
                    </Columns>
                </asp:GridView>
            </div>
        </asp:Panel>

        <div class="card mb-4">
            <div class="card-header">操作</div>
            <div class="card-body">
                <div class="mb-3">
                    <label for="txtActionComments" class="form-label">簽核/駁回意見</label>
                    <asp:TextBox ID="txtActionComments" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2"></asp:TextBox>
                </div>
                <hr />

                <asp:Panel ID="pnlRequesterManagerAction" runat="server" Visible="false">
                    <h5>開單主管審核</h5>
                    <asp:Button ID="btnReqManagerApprove" runat="server" Text="同意" OnClick="btnReqManagerApprove_Click" CssClass="btn btn-success me-2" />
                    <asp:Button ID="btnReqManagerReject" runat="server" Text="駁回" OnClick="btnAction_Reject" CssClass="btn btn-danger" />
                </asp:Panel>

                <asp:Panel ID="pnlSignOffAction" runat="server" Visible="false">
                    <h5>我的會簽</h5>
                    <asp:Button ID="btnSignOffApprove" runat="server" Text="同意" OnClick="btnSignOffApprove_Click" CssClass="btn btn-success me-2" />
                    <asp:Button ID="btnSignOffReject" runat="server" Text="駁回" OnClick="btnAction_Reject" CssClass="btn btn-danger" />
                </asp:Panel>
                
                <asp:Panel ID="pnlCimBossAction" runat="server" Visible="false">
                    <h5>CIM 主管審核</h5>
                    <asp:Button ID="btnCimBossApprove" runat="server" Text="同意" OnClick="btnCimBossApprove_Click" CssClass="btn btn-success me-2" />
                    <asp:Button ID="btnCimBossReject" runat="server" Text="駁回" OnClick="btnAction_Reject" CssClass="btn btn-danger" />
                </asp:Panel>

                <asp:Panel ID="pnlCimLeaderAction" runat="server" Visible="false">
                    <h5>CIM 主任指派</h5>
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label for="ddlEngineers" class="form-label">選擇工程師</label>
                            <asp:DropDownList ID="ddlEngineers" runat="server" CssClass="form-select" DataTextField="Username" DataValueField="UserID"></asp:DropDownList>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label for="txtPlannedDate" class="form-label">預計完成日期</label>
                            <asp:TextBox ID="txtPlannedDate" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                        </div>
                    </div>
                    <asp:Button ID="btnAssign" runat="server" Text="指派" OnClick="btnAssign_Click" CssClass="btn btn-primary" />
                </asp:Panel>

                <asp:Panel ID="pnlEngineerAction" runat="server" Visible="false">
                    <h5>工程師操作</h5>
                    <asp:Button ID="btnAcceptSR" runat="server" Text="確認接單" OnClick="btnAcceptSR_Click" CssClass="btn btn-primary me-2" />
                    <asp:Button ID="btnCompleteDev" runat="server" Text="完成開發" OnClick="btnCompleteDev_Click" CssClass="btn btn-info me-2" />
                    <asp:Button ID="btnDeploy" runat="server" Text="程式上線" OnClick="btnDeploy_Click" CssClass="btn btn-warning me-2" />
                    <asp:Button ID="btnConfirmClosure" runat="server" Text="確認結單" OnClick="btnConfirmClosure_Click" CssClass="btn btn-success" />
                </asp:Panel>
                
                 <asp:Panel ID="pnlUserAction" runat="server" Visible="false">
                    <h5>需求者操作</h5>
                    <div class="mb-3">
                        <label for="fileUploadClosureReport" class="form-label">上傳測試報告</label>
                        <asp:FileUpload ID="fileUploadClosureReport" runat="server" AllowMultiple="false" CssClass="form-control" />
                    </div>
                    <asp:Button ID="btnUploadClosureReport" runat="server" Text="上傳並通知工程師" OnClick="btnUploadClosureReport_Click" CssClass="btn btn-primary" />
                </asp:Panel>
            </div>
        </div>

        <div class="card">
            <div class="card-header">歷史紀錄</div>
            <div class="card-body">
                 <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="False" CssClass="table table-sm">
                    <Columns>
                        <asp:BoundField DataField="ActionDate" HeaderText="日期" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                        <asp:BoundField DataField="Username" HeaderText="操作者" />
                        <asp:BoundField DataField="Action" HeaderText="操作" />
                        <asp:BoundField DataField="Notes" HeaderText="備註" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>

    </asp:Panel>
</asp:Content>
