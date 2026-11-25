<%@ Page Title="Admin - Products" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Products.aspx.cs" Inherits="LegacyShop.Web.Admin.Products" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Products</h1>
    <asp:Label ID="MessageLabel" runat="server" CssClass="message" EnableViewState="false" />

    <asp:GridView ID="AdminGrid" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" CssClass="grid"
        OnRowEditing="AdminGrid_RowEditing" OnRowCancelingEdit="AdminGrid_RowCancelingEdit"
        OnRowUpdating="AdminGrid_RowUpdating" OnRowDeleting="AdminGrid_RowDeleting">
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Id" ReadOnly="true" />
            <asp:TemplateField HeaderText="Name">
                <ItemTemplate><%#: Eval("Name") %></ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="NameBox" runat="server" Text='<%# Bind("Name") %>' />
                    <asp:RequiredFieldValidator ID="NameRequired" runat="server" ControlToValidate="NameBox" ErrorMessage="Name is required" ValidationGroup="Edit" Display="Dynamic" CssClass="error" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Price">
                <ItemTemplate><%# Eval("Price", "{0:0.00}") %></ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="PriceBox" runat="server" Text='<%# Bind("Price", "{0:0.00}") %>' Width="80" />
                    <asp:RangeValidator ID="PriceRange" runat="server" ControlToValidate="PriceBox" Type="Currency" MinimumValue="0" MaximumValue="100000" ErrorMessage="Invalid price" ValidationGroup="Edit" Display="Dynamic" CssClass="error" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Stock">
                <ItemTemplate><%# Eval("Stock") %></ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="StockBox" runat="server" Text='<%# Bind("Stock") %>' Width="60" />
                    <asp:RangeValidator ID="StockRange" runat="server" ControlToValidate="StockBox" Type="Integer" MinimumValue="0" MaximumValue="100000" ErrorMessage="Invalid stock" ValidationGroup="Edit" Display="Dynamic" CssClass="error" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Active">
                <ItemTemplate><asp:CheckBox ID="ActiveView" runat="server" Checked='<%# Eval("IsActive") %>' Enabled="false" /></ItemTemplate>
                <EditItemTemplate><asp:CheckBox ID="ActiveBox" runat="server" Checked='<%# Bind("IsActive") %>' /></EditItemTemplate>
            </asp:TemplateField>
            <asp:CommandField ShowEditButton="true" ShowDeleteButton="true" ValidationGroup="Edit" />
        </Columns>
    </asp:GridView>

    <h2>Add product</h2>
    <asp:Panel ID="AddPanel" runat="server" DefaultButton="AddButton">
        <p>
            Name <asp:TextBox ID="NewName" runat="server" />
            <asp:RequiredFieldValidator ID="NewNameRequired" runat="server" ControlToValidate="NewName" ErrorMessage="Name is required" ValidationGroup="Add" Display="Dynamic" CssClass="error" />
        </p>
        <p>
            Price <asp:TextBox ID="NewPrice" runat="server" Width="80" />
            <asp:RequiredFieldValidator ID="NewPriceRequired" runat="server" ControlToValidate="NewPrice" ErrorMessage="Price is required" ValidationGroup="Add" Display="Dynamic" CssClass="error" />
            <asp:RangeValidator ID="NewPriceRange" runat="server" ControlToValidate="NewPrice" Type="Currency" MinimumValue="0" MaximumValue="100000" ErrorMessage="Invalid price" ValidationGroup="Add" Display="Dynamic" CssClass="error" />
        </p>
        <p>Category <asp:DropDownList ID="NewCategory" runat="server" /></p>
        <p><asp:Button ID="AddButton" runat="server" Text="Add" OnClick="AddButton_Click" ValidationGroup="Add" /></p>
    </asp:Panel>
</asp:Content>
