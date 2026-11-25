<%@ Page Title="Product" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Product.aspx.cs" Inherits="LegacyShop.Web.ProductPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="NotFoundPanel" runat="server" Visible="false">
        <h1>Product not found</h1>
        <p><a href="Default.aspx">Back to products</a></p>
    </asp:Panel>
    <asp:Panel ID="DetailsPanel" runat="server">
        <h1><asp:Literal ID="NameLiteral" runat="server" /></h1>
        <asp:Image ID="ProductImage" runat="server" AlternateText="Product image" />
        <p class="muted"><asp:Literal ID="CategoryLiteral" runat="server" /></p>
        <p><asp:Literal ID="DescriptionLiteral" runat="server" /></p>
        <p class="price"><asp:Literal ID="PriceLiteral" runat="server" /></p>
        <p><asp:Label ID="StockLabel" runat="server" /></p>

        <asp:UpdatePanel ID="CartUpdatePanel" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:TextBox ID="QuantityBox" runat="server" Text="1" Width="50" />
                <asp:RequiredFieldValidator ID="QuantityRequired" runat="server" ControlToValidate="QuantityBox" ErrorMessage="Enter a quantity" Display="Dynamic" CssClass="error" />
                <asp:RangeValidator ID="QuantityRange" runat="server" ControlToValidate="QuantityBox" Type="Integer" MinimumValue="1" MaximumValue="20" ErrorMessage="1 to 20 only" Display="Dynamic" CssClass="error" />
                <asp:Button ID="AddToCartButton" runat="server" Text="Add to cart" OnClick="AddToCartButton_Click" />
                <asp:Label ID="AddedLabel" runat="server" CssClass="message" EnableViewState="false" />
            </ContentTemplate>
        </asp:UpdatePanel>
    </asp:Panel>
</asp:Content>
