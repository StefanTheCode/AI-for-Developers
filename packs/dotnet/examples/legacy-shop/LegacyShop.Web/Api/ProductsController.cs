using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Http;
using LegacyShop.Data;
using LegacyShop.Data.Entities;
using LegacyShop.Web.Services;

namespace LegacyShop.Web.Api
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
    }

    public class ProductInput
    {
        [Required, StringLength(120)]
        public string Name { get; set; }

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }

    // TRAP T19: ApiController, IHttpActionResult, [FromUri], ModelState and attribute routes.
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ProductRepository _products = new ProductRepository();

        [HttpGet, Route("")]
        public IHttpActionResult GetAll([FromUri] int? categoryId = null)
        {
            var items = _products.GetActive(categoryId, "Name").Select(ToDto).ToList();
            return Ok(items);
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            var product = _products.Find(id);
            if (product == null || !product.IsActive) return NotFound();
            return Ok(ToDto(product));
        }

        [HttpGet, Route("{id:int}/stock")]
        public IHttpActionResult Stock(int id)
        {
            var stock = InventoryClient.GetStock(id);
            return Ok(new { stock.ProductId, stock.Available, stock.LowStock });
        }

        // TRAP T19b: with Forms auth on, an unauthenticated call gets a 302 redirect to Login.aspx, not a 401.
        [HttpPost, Route(""), Authorize(Roles = "Admin")]
        public IHttpActionResult Create(ProductInput input)
        {
            if (input == null) return BadRequest("Body is required.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using (var db = new ShopContext())
            {
                var product = new Product { Name = input.Name, Price = input.Price, CategoryId = input.CategoryId, IsActive = true, Stock = 0 };
                db.Products.Add(product);
                db.SaveChanges();
                return Created("api/products/" + product.Id, new { product.Id });
            }
        }

        private static ProductDto ToDto(Product p)
        {
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Category = p.Category != null ? p.Category.Name : null,
                InStock = p.Stock > 0
            };
        }
    }
}
