<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MiniCart.ascx.cs" Inherits="LegacyShop.Web.Controls.MiniCart" %>
<span class="minicart">
    <asp:HyperLink ID="CartLink" runat="server" NavigateUrl="~/Cart.aspx">Cart (<asp:Literal ID="CountLiteral" runat="server" />)</asp:HyperLink>
    <asp:Literal ID="TotalLiteral" runat="server" />
</span>
