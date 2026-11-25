<%@ Page Title="Sign in" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LegacyShop.Web.Account.Login" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Sign in</h1>
    <asp:Panel ID="LoginPanel" runat="server" DefaultButton="LoginButton">
        <p>
            Email<br />
            <asp:TextBox ID="EmailBox" runat="server" Width="260" />
            <asp:RequiredFieldValidator ID="EmailRequired" runat="server" ControlToValidate="EmailBox" ErrorMessage="Email is required" Display="Dynamic" CssClass="error" />
        </p>
        <p>
            Password<br />
            <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" Width="260" />
            <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="PasswordBox" ErrorMessage="Password is required" Display="Dynamic" CssClass="error" />
        </p>
        <p><asp:CheckBox ID="RememberMe" runat="server" Text="Keep me signed in" /></p>
        <p><asp:Button ID="LoginButton" runat="server" Text="Sign in" OnClick="LoginButton_Click" /></p>
        <asp:Label ID="ErrorLabel" runat="server" CssClass="error" EnableViewState="false" />
        <p class="muted">Demo users: admin@legacyshop.local / admin123 and ana@legacyshop.local / ana123</p>
    </asp:Panel>
</asp:Content>
