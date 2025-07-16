<%-- 
================================================================================
檔案：/CreateSR.aspx
變更：修正了 Repeater 中的資料繫結語法，將 Eval("PropertyName") 改為 Eval("[KeyName]")，
      以正確地從 Dictionary 中讀取資料。
================================================================================
--%>
<%@ Page Title="建立新 SR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CreateSR.aspx.cs" Inherits="SR_System.CreateSR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PrimaryContent" runat="server">
    <h3>建立新的服務請求 (SR)</h3>
    <hr />

    <asp:Literal ID="litMessage" runat="server"></asp:Literal>

    <div class="card mb-4">
        <div class="card-header">SR 基本資訊</div>
        <div class="card-body">
            <div class="mb-3">
                <label for="txtTitle" class="form-label">標題 <span class="text-danger">*</span></label>
                <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtTitle" ErrorMessage="標題為必填欄位。" CssClass="text-danger" Display="Dynamic" ValidationGroup="SRValidation" />
            </div>
            <div class="mb-3">
                <label for="txtPurpose" class="form-label">目的 <span class="text-danger">*</span></label>
                <asp:TextBox ID="txtPurpose" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPurpose" ErrorMessage="目的為必填欄位。" CssClass="text-danger" Display="Dynamic" ValidationGroup="SRValidation" />
            </div>
            <div class="mb-3">
                <label for="txtScope" class="form-label">範圍 <span class="text-danger">*</span></label>
                <asp:TextBox ID="txtScope" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtScope" ErrorMessage="範圍為必填欄位。" CssClass="text-danger" Display="Dynamic" ValidationGroup="SRValidation" />
            </div>
            <div class="mb-3">
                <label for="txtBenefit" class="form-label">效益 <span class="text-danger">*</span></label>
                <asp:TextBox ID="txtBenefit" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtBenefit" ErrorMessage="效益為必填欄位。" CssClass="text-danger" Display="Dynamic" ValidationGroup="SRValidation" />
            </div>
            <div class="mb-3">
                <label for="fileUploadInitialDocs" class="form-label">上傳需求書 (可多選) <span class="text-danger">*</span></label>
                <asp:FileUpload ID="fileUploadInitialDocs" runat="server" AllowMultiple="true" CssClass="form-control" />
                <asp:CustomValidator runat="server" ID="cvFileUpload" ControlToValidate="fileUploadInitialDocs" ErrorMessage="請至少上傳一個檔案。" OnServerValidate="ValidateFileUpload" CssClass="text-danger" Display="Dynamic" ValidationGroup="SRValidation" />
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">會簽人員設定</div>
        <div class="card-body">
            <asp:UpdatePanel ID="upApprovers" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <div class="row g-3 align-items-center">
                        <div class="col-md-8">
                            <label for="txtApproverSearch" class="form-label">搜尋會簽人員 (輸入姓名或工號)</label>
                            <input type="text" id="txtApproverSearch" runat="server" class="form-control" />
                            <asp:HiddenField ID="hdnApproverUserId" runat="server" />
                        </div>
                        <div class="col-md-4 align-self-end">
                            <asp:Button ID="btnAddApprover" runat="server" Text="加入會簽" OnClick="btnAddApprover_Click" CssClass="btn btn-secondary w-100" />
                        </div>
                    </div>
                    <hr />
                    <h5>已加入的會簽人員</h5>
                    <asp:Repeater ID="rptApprovers" runat="server" OnItemCommand="rptApprovers_ItemCommand">
                        <HeaderTemplate>
                            <table class="table table-sm table-bordered">
                                <thead class="table-light">
                                    <tr>
                                        <th>姓名</th>
                                        <th>工號</th>
                                        <th>操作</th>
                                    </tr>
                                </thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <%-- (關鍵修正) 使用 Eval("[KeyName]") 語法來存取 Dictionary 的值 --%>
                                <td><%# Eval("[Username]") %></td>
                                <td><%# Eval("[EmployeeID]") %></td>
                                <td>
                                    <asp:Button ID="btnRemove" runat="server" Text="移除" CommandName="Remove" CommandArgument='<%# Eval("[UserID]") %>' CssClass="btn btn-danger btn-sm" />
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                                </tbody>
                            </table>
                        </FooterTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlEmptyApprovers" runat="server" Visible="false">
                        <div class="alert alert-info">尚未加入任何會簽人員。</div>
                    </asp:Panel>
                </ContentTemplate>
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="btnAddApprover" EventName="Click" />
                </Triggers>
            </asp:UpdatePanel>
        </div>
    </div>
    
    <div class="mt-4 text-end">
        <asp:Button ID="btnSubmit" runat="server" Text="提交服務請求" OnClick="btnSubmit_Click" CssClass="btn btn-success btn-lg" ValidationGroup="SRValidation" />
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
<script type="text/javascript">
    function pageLoad() {
        $("#<%= txtApproverSearch.ClientID %>").autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: "SearchUsers.ashx",
                    data: { term: request.term },
                    dataType: "json",
                    type: "GET",
                    success: function (data) {
                        response($.map(data, function (item) {
                            return {
                                label: item.Username + ' (' + item.EmployeeID + ')',
                                value: item.Username,
                                userId: item.UserID
                            };
                        }));
                    },
                    error: function (xhr, status, error) {
                        console.error("Autocomplete error: " + error);
                    }
                });
            },
            minLength: 2,
            select: function (event, ui) {
                $("#<%= hdnApproverUserId.ClientID %>").val(ui.item.userId);
                $(this).val(ui.item.label);
                return false;
            }
        });

        $("#<%= txtApproverSearch.ClientID %>").on("input", function() {
            if ($(this).val() === "") {
                $("#<%= hdnApproverUserId.ClientID %>").val("");
            }
        });
    }
</script>
</asp:Content>
