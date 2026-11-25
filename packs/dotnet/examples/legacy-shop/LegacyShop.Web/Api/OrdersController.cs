using System.Linq;
using System.Web.Http;
using LegacyShop.Core;
using LegacyShop.Data;

namespace LegacyShop.Web.Api
{
    [Authorize, RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        [HttpGet, Route("mine")]
        public IHttpActionResult Mine()
        {
            // TRAP T20: Web API reaching into HttpContext.Current through the static CurrentUser helper.
            var customerId = CurrentUser.CustomerId;
            if (customerId == null) return Unauthorized();

            // TRAP T25: ForCustomer leaves the context open; o.Lines and l.Product lazy load = N+1 queries.
            var orders = new OrderRepository().ForCustomer(customerId.Value);
            var result = orders.Select(o => new
            {
                o.Id,
                o.CreatedAt,
                o.Status,
                o.Total,
                Lines = o.Lines.Select(l => new { Product = l.Product.Name, l.Quantity, l.UnitPrice })
            }).ToList();
            return Ok(result);
        }
    }
}
