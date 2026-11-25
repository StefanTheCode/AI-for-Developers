<%@ Page Title="Cart" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Cart.aspx.cs" Inherits="LegacyShop.Web.CartPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Your cart</h1>
    <asp:Label ID="ConfirmationLabel" runat="server" CssClass="message" EnableViewState="false" />
    <asp:Label ID="ErrorLabel" runat="server" CssClass="error" EnableViewState="false" />

    <asp:Panel ID="EmptyPanel" runat="server">
        <p class="muted">Your cart is empty. <a href="Default.aspx">Browse products</a></p>
    </asp:Panel>

    <asp:Panel ID="CartPanel" runat="server">
        <table class="grid">
            <tr><th>Product</th><th>Price</th><th>Quantity</th><th></th></tr>
            <asp:Repeater ID="LinesRepeater" runat="server" OnItemCommand="LinesRepeater_ItemCommand">
                <ItemTemplate>
                    <tr>
                        <td><%#: Eval("Name") %></td>
                        <td><%# Eval("UnitPrice", "{0:0.00}") %></td>
                        <td>
                            <asp:HiddenField ID="ProductIdField" runat="server" Value='<%# Eval("ProductId") %>' />
                            <asp:TextBox ID="QtyBox" runat="server" Text='<%# Eval("Quantity") %>' Width="50" />
                        </td>
                        <td><asp:LinkButton ID="RemoveButton" runat="server" CommandName="Remove" CommandArgument='<%# Eval("ProductId") %>' Text="Remove" CausesValidation="false" /></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>
        <p><asp:Button ID="UpdateButton" runat="server" Text="Update quantities" OnClick="UpdateButton_Click" /></p>

        <p>
            Coupon: <asp:TextBox ID="CouponBox" runat="server" Width="120" />
            <asp:Button ID="ApplyCouponButton" runat="server" Text="Apply" OnClick="ApplyCouponButton_Click" />
        </p>

        <table class="grid" style="width:auto">
            <tr><td>Subtotal</td><td><asp:Literal ID="SubtotalLiteral" runat="server" /></td></tr>
            <tr><td>Discount</td><td><asp:Literal ID="DiscountLiteral" runat="server" /></td></tr>
            <tr><td>Shipping</td><td><asp:Literal ID="ShippingLiteral" runat="server" /></td></tr>
            <tr><td>VAT</td><td><asp:Literal ID="VatLiteral" runat="server" /></td></tr>
            <tr><td><strong>Total</strong></td><td><strong><asp:Literal ID="TotalLiteral" runat="server" /></strong></td></tr>
        </table>
        <p><asp:Button ID="CheckoutButton" runat="server" Text="Checkout" OnClick="CheckoutButton_Click" /></p>
    </asp:Panel>
</asp:Content>
