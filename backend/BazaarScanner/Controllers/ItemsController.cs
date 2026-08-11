using BazaarScanner.Data;
using BazaarScanner.Models;
using BazaarScanner.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BazaarScanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly AppDbContext _appDbContext;

        public ItemsController(GeminiService geminiService, AppDbContext appDbContext)
        {
            _geminiService = geminiService;
            _appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var myItems = await _appDbContext.Items.ToListAsync();
            return Ok(myItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] ScannedItem newItem)
        {
            if (string.IsNullOrEmpty(newItem.Id))
            {
                newItem.Id = Guid.NewGuid().ToString();
            }

            _appDbContext.Items.Add(newItem);
            await _appDbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllItems), new { id = newItem.Id }, newItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(string id, [FromBody] ScannedItem updatedItem)
        {
            if (id != updatedItem.Id)
            {
                return BadRequest("ID in URL does not match ID in body.");
            }

            var existingItem = await _appDbContext.Items.FindAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }
            existingItem.Name = updatedItem.Name;
            existingItem.Type = updatedItem.Type;
            existingItem.Count = updatedItem.Count;
            existingItem.ImageUrl = updatedItem.ImageUrl;

            await _appDbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("scan")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ScanItemAsync([FromForm] ImageUploadRequest request)
        {
            var image = request.Image;
            if (image == null || image.Length == 0) return BadRequest("No image uploaded.");

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);

            var scannedItem = await _geminiService.GetContentFromImage(memoryStream.ToArray(), image.ContentType);
            if (scannedItem == null) return BadRequest("Failed to scan item.");

            scannedItem.Id = Guid.NewGuid().ToString();
            scannedItem.Count = 1;

            return Ok(scannedItem);
        }
        public class ImageUploadRequest
        {
            public IFormFile Image { get; set; } = null!;
        }
    }
}