using System;
using System.Linq;
using System.Web.UI;
using LegacyShop.Web.Infrastructure;

namespace LegacyShop.Web.Controls
{
    // TRAP T06: a user control that reads Session directly. It sits outside the UpdatePanel on
    // Product.aspx, so it does NOT refresh after "Add to cart" until the next full page load.
    public partial class MiniCart : UserControl
    {
        protected void Page_PreRender(object sender, EventArgs e)
        {
            var lines = CartSession.Lines;
            CountLiteral.Text = CartSession.Count.ToString();
            TotalLiteral.Text = lines.Count == 0 ? "" : lines.Sum(l => l.UnitPrice * l.Quantity).ToString("0.00");
        }
    }
}
