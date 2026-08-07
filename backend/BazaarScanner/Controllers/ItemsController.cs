using BazaarScanner.Models;
using Microsoft.AspNetCore.Mvc;

namespace BazaarScanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private static List<ScannedItem> myItems = new()
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "test1", Type = ScannedItemType.Toy, Count = 1 },
            new() { Id = Guid.NewGuid().ToString(), Name = "test2", Type = ScannedItemType.Electronic, Count = 2 }
        };

        [HttpGet]
        public IActionResult GetAllItems()
        {
            return Ok(myItems);
        }

        [HttpPost]
        public IActionResult AddItem([FromBody] ScannedItem newItem)
        {
            newItem.Id = Guid.NewGuid().ToString();

            myItems.Add(newItem);

            return CreatedAtAction(nameof(GetAllItems), new { id = newItem.Id }, newItem);
        }
    }
}