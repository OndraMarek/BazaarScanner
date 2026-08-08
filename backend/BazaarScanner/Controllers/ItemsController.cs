using BazaarScanner.Models;
using BazaarScanner.Services;
using Microsoft.AspNetCore.Mvc;

namespace BazaarScanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        public ItemsController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

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

        [HttpPost("scan")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ScanItemAsync([FromForm] ImageUploadRequest request)
        {
            var image = request.Image;

            if (image == null || image.Length == 0)
            {
                return BadRequest("No image uploaded.");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var scannedItem = await _geminiService.GetContentFromImage(imageBytes, image.ContentType);

            if (scannedItem == null)
            {
                return BadRequest("Failed to scan item.");
            }

            scannedItem.Id = Guid.NewGuid().ToString();
            scannedItem.Count = 1;
            myItems.Add(scannedItem);

            return Ok(scannedItem);
        }
    }
    public class ImageUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
    }
}