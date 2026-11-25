using System;
using System.Linq;
using System.ServiceModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using LegacyShop.Core;
using LegacyShop.Data;
using LegacyShop.Data.Entities;
using LegacyShop.Web.Infrastructure;
using LegacyShop.Web.Services;

namespace LegacyShop.Web
{
    public partial class CartPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            if (Request.QueryString["ordered"] != null)
            {
                ConfirmationLabel.Text = "Thanks! Your order #" + Server.HtmlEncode(Request.QueryString["ordered"]) + " is in.";
            }
            Bind();
        }

        private void Bind()
        {
            var lines = CartSession.Lines;
            LinesRepeater.DataSource = lines;
            LinesRepeater.DataBind();
            EmptyPanel.Visible = lines.Count == 0;
            CartPanel.Visible = lines.Count > 0;

            var price = new PriceCalculator().Calculate(lines, CouponBox.Text);
            SubtotalLiteral.Text = price.Subtotal.ToString("0.00");
            DiscountLiteral.Text = price.Discount.ToString("0.00");
            ShippingLiteral.Text = price.Shipping.ToString("0.00");
            VatLiteral.Text = price.Vat.ToString("0.00");
            TotalLiteral.Text = price.Total.ToString("0.00");
        }

        protected void LinesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Remove") return;
            CartSession.Remove(int.Parse((string)e.CommandArgument));
            Bind();
        }

        protected void UpdateButton_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in LinesRepeater.Items)
            {
                var idField = (HiddenField)item.FindControl("ProductIdField");
                var qtyBox = (TextBox)item.FindControl("QtyBox");
                int quantity;
                if (int.TryParse(qtyBox.Text, out quantity))
                {
                    CartSession.SetQuantity(int.Parse(idField.Value), quantity);
                }
            }
            Bind();
        }

        protected void ApplyCouponButton_Click(object sender, EventArgs e)
        {
            Bind();
        }

        protected void CheckoutButton_Click(object sender, EventArgs e)
        {
            if (!CurrentUser.IsAuthenticated)
            {
                Response.Redirect("~/Account/Login.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl));
                return;
            }

            var lines = CartSession.Lines.ToList();
            if (lines.Count == 0) return;

            try
            {
                foreach (var line in lines)
                {
                    InventoryClient.Reserve(line.ProductId, line.Quantity);
                }
            }
            catch (FaultException<InventoryFault> fault)
            {
                ErrorLabel.Text = Server.HtmlEncode(fault.Detail.Message);
                Bind();
                return;
            }

            // QUIRK: the stored order total is the plain sum of lines - no coupon, no shipping, no VAT.
            // The cart page shows a different total than the order history. Parity tests will catch it.
            var order = new OrderRepository().Create(
                CurrentUser.CustomerId.Value,
                lines.Select(l => new OrderLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }));

            CartSession.Clear();
            Response.Redirect("~/Cart.aspx?ordered=" + order.Id);
        }
    }
}
