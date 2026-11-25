using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using LegacyShop.Data;
using LegacyShop.Data.Entities;

namespace LegacyShop.Web.Admin
{
    // TRAP T07 + T08: GridView inline editing with validators, and the code-behind talks to the
    // DbContext directly - no service layer to move first. Access control lives in Web.config (T17).
    public partial class Products : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            using (var db = new ShopContext())
            {
                NewCategory.DataSource = db.Categories.OrderBy(c => c.Name).ToList();
                NewCategory.DataTextField = "Name";
                NewCategory.DataValueField = "Id";
                NewCategory.DataBind();
            }
            Bind();
        }

        private void Bind()
        {
            using (var db = new ShopContext())
            {
                AdminGrid.DataSource = db.Products.OrderBy(p => p.Name).ToList();
                AdminGrid.DataBind();
            }
        }

        protected void AdminGrid_RowEditing(object sender, GridViewEditEventArgs e)
        {
            AdminGrid.EditIndex = e.NewEditIndex;
            Bind();
        }

        protected void AdminGrid_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            AdminGrid.EditIndex = -1;
            Bind();
        }

        protected void AdminGrid_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (!Page.IsValid) return;
            var row = AdminGrid.Rows[e.RowIndex];
            var id = (int)AdminGrid.DataKeys[e.RowIndex].Value;

            using (var db = new ShopContext())
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    product.Name = ((TextBox)row.FindControl("NameBox")).Text.Trim();
                    // QUIRK: parsed with the server's current culture - "12,50" vs "12.50" depends on the machine.
                    product.Price = decimal.Parse(((TextBox)row.FindControl("PriceBox")).Text, CultureInfo.CurrentCulture);
                    product.Stock = int.Parse(((TextBox)row.FindControl("StockBox")).Text);
                    product.IsActive = ((CheckBox)row.FindControl("ActiveBox")).Checked;
                    db.SaveChanges();
                    MessageLabel.Text = "Saved " + Server.HtmlEncode(product.Name) + ".";
                }
            }

            AdminGrid.EditIndex = -1;
            Bind();
        }

        protected void AdminGrid_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            var id = (int)AdminGrid.DataKeys[e.RowIndex].Value;
            using (var db = new ShopContext())
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    // TRAP T26b: OrderLine.ProductId is a required FK, so EF6 convention cascades the delete
                    // and silently removes order history for this product.
                    db.Products.Remove(product);
                    db.SaveChanges();
                    MessageLabel.Text = "Deleted product " + id + ".";
                }
            }
            Bind();
        }

        protected void AddButton_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            using (var db = new ShopContext())
            {
                db.Products.Add(new Product
                {
                    Name = NewName.Text.Trim(),
                    Price = decimal.Parse(NewPrice.Text, CultureInfo.CurrentCulture),
                    CategoryId = int.Parse(NewCategory.SelectedValue),
                    IsActive = true,
                    Stock = 0
                });
                db.SaveChanges();
            }
            NewName.Text = "";
            NewPrice.Text = "";
            MessageLabel.Text = "Product added.";
            Bind();
        }
    }
}
