<%@ Page Title="Products" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LegacyShop.Web._Default" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Products</h1>
    <p>
        <asp:DropDownList ID="CategoryList" runat="server" AutoPostBack="true" OnSelectedIndexChanged="CategoryList_SelectedIndexChanged" />
        <asp:Label ID="ResultCount" runat="server" CssClass="muted" />
    </p>
    <asp:GridView ID="ProductsGrid" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" CssClass="grid"
        AllowPaging="true" PageSize="10" AllowSorting="true"
        OnPageIndexChanging="ProductsGrid_PageIndexChanging" OnSorting="ProductsGrid_Sorting">
        <Columns>
            <asp:ImageField DataImageUrlField="Id" DataImageUrlFormatString="~/Handlers/ProductImage.ashx?id={0}" HeaderText="" ControlStyle-Width="60" />
            <asp:HyperLinkField DataTextField="Name" DataNavigateUrlFields="Id" DataNavigateUrlFormatString="~/Product.aspx?id={0}" HeaderText="Name" SortExpression="Name" />
            <asp:TemplateField HeaderText="Category">
                <ItemTemplate><%#: Eval("Category.Name") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:0.00}" SortExpression="Price" />
        </Columns>
        <EmptyDataTemplate><p class="muted">No products in this category.</p></EmptyDataTemplate>
    </asp:GridView>
</asp:Content>
