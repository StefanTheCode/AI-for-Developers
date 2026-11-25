using System;
using System.Web.UI;
using LegacyShop.Data;
using LegacyShop.Web.Infrastructure;
using LegacyShop.Web.Services;

namespace LegacyShop.Web
{
    // TRAP T01 + T02: product id, name and the price AT LOAD TIME live in ViewState, and "Add to cart"
    // runs as an UpdatePanel partial postback. The cart gets the ViewState price, not the current one.
    public partial class ProductPage : Page
    {
        private int ProductId
        {
            get { return (int)(ViewState["ProductId"] ?? 0); }
            set { ViewState["ProductId"] = value; }
        }

        private string ProductName
        {
            get { return (string)ViewState["ProductName"]; }
            set { ViewState["ProductName"] = value; }
        }

        private decimal PriceAtLoad
        {
            get { return (decimal)(ViewState["PriceAtLoad"] ?? 0m); }
            set { ViewState["PriceAtLoad"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            int id;
            if (!int.TryParse(Request.QueryString["id"], out id))
            {
                Response.Redirect("~/Default.aspx");
                return;
            }

            var product = new ProductRepository().Find(id);
            if (product == null || !product.IsActive)
            {
                Response.StatusCode = 404;
                NotFoundPanel.Visible = true;
                DetailsPanel.Visible = false;
                return;
            }

            ProductId = product.Id;
            ProductName = product.Name;
            PriceAtLoad = product.Price;

            Title = product.Name;
            NameLiteral.Text = Server.HtmlEncode(product.Name);
            CategoryLiteral.Text = Server.HtmlEncode(product.Category.Name);
            DescriptionLiteral.Text = Server.HtmlEncode(product.Description);
            PriceLiteral.Text = product.Price.ToString("0.00");
            ProductImage.ImageUrl = "~/Handlers/ProductImage.ashx?id=" + product.Id;
            StockLabel.Text = StockText(product.Id);
        }

        private static string StockText(int productId)
        {
            try
            {
                var stock = InventoryClient.GetStock(productId);
                if (stock.Available == 0) return "Out of stock";
                return stock.LowStock ? "Only " + stock.Available + " left" : "In stock";
            }
            catch (Exception)
            {
                // The WCF service is down more often than anyone admits.
                return "Stock unknown";
            }
        }

        protected void AddToCartButton_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            var quantity = int.Parse(QuantityBox.Text);
            CartSession.Add(ProductId, ProductName, PriceAtLoad, quantity);
            AddedLabel.Text = "Added " + quantity + " to your cart.";
        }
    }
}
