using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using LegacyShop.Data;

namespace LegacyShop.Web
{
    // TRAP T03: AutoPostBack dropdown + GridView paging/sorting as server events, sort kept in ViewState (T01).
    public partial class _Default : Page
    {
        private readonly ProductRepository _products = new ProductRepository();

        private string SortBy
        {
            get { return (string)ViewState["SortBy"] ?? "Name"; }
            set { ViewState["SortBy"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CategoryList.DataSource = _products.GetCategories();
            CategoryList.DataTextField = "Name";
            CategoryList.DataValueField = "Id";
            CategoryList.DataBind();
            CategoryList.Items.Insert(0, new ListItem("All categories", ""));

            int categoryId;
            if (int.TryParse(Request.QueryString["cat"], out categoryId))
            {
                var item = CategoryList.Items.FindByValue(categoryId.ToString());
                if (item != null) CategoryList.SelectedValue = item.Value;
            }

            BindGrid();
        }

        private void BindGrid()
        {
            int id;
            int? categoryId = int.TryParse(CategoryList.SelectedValue, out id) ? id : (int?)null;
            var products = _products.GetActive(categoryId, SortBy);
            ProductsGrid.DataSource = products;
            ProductsGrid.DataBind();
            ResultCount.Text = products.Count + " products";
        }

        protected void CategoryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProductsGrid.PageIndex = 0;
            BindGrid();
        }

        protected void ProductsGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ProductsGrid.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void ProductsGrid_Sorting(object sender, GridViewSortEventArgs e)
        {
            // QUIRK: "Name DESC" isn't handled by the repository, so a second click on Name keeps A-Z.
            SortBy = SortBy == e.SortExpression ? e.SortExpression + " DESC" : e.SortExpression;
            BindGrid();
        }
    }
}
