using System.Web.Mvc;
using LegacyShop.Data;

namespace LegacyShop.Web.Controllers
{
    // TRAP T21: MVC 5 controller with ViewBag, OutputCache and a [ChildActionOnly] child action (Html.Action).
    // ASP.NET Core has no child actions - they become view components.
    public class ReportsController : Controller
    {
        private readonly OrderRepository _orders = new OrderRepository();

        [OutputCache(Duration = 60, VaryByParam = "days")]
        public ActionResult Index(int days = 30)
        {
            ViewBag.Days = days;
            return View(_orders.SalesByDay(days));
        }

        [ChildActionOnly]
        public ActionResult TopProducts(int take = 5)
        {
            return PartialView("_TopProducts", _orders.TopProducts(take));
        }
    }
}
