using BazaarScanner.Data;
using BazaarScanner.DTOs;
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
        private readonly IWebHostEnvironment _environment;

        public ItemsController(GeminiService geminiService, AppDbContext appDbContext, IWebHostEnvironment environment)
        {
            _geminiService = geminiService;
            _appDbContext = appDbContext;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var myItems = await _appDbContext.Items.ToListAsync();
            var dtos = myItems.Select(item => MapToResponseDto(item)).ToList();
            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] CreateItemDto dto)
        {
            var newItem = new ScannedItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name,
                Type = dto.Type,
                Count = dto.Count,
                ImageUrl = dto.ImageUrl
            };

            _appDbContext.Items.Add(newItem);
            await _appDbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllItems), new { id = newItem.Id }, MapToResponseDto(newItem));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(string id, [FromBody] UpdateItemDto dto)
        {
            var existingItem = await _appDbContext.Items.FindAsync(id);
            if (existingItem == null) return NotFound();

            existingItem.Name = dto.Name;
            existingItem.Type = dto.Type;
            existingItem.Count = dto.Count;
            existingItem.ImageUrl = dto.ImageUrl;

            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(string id)
        {
            var existingItem = await _appDbContext.Items.FindAsync(id);
            if (existingItem == null) return NotFound();

            if (!string.IsNullOrEmpty(existingItem.ImageUrl))
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var imagePath = existingItem.ImageUrl.TrimStart('/', '\\');
                var fullFilePath = Path.Combine(webRootPath, imagePath);

                if (System.IO.File.Exists(fullFilePath))
                {
                    System.IO.File.Delete(fullFilePath);
                }
            }

            _appDbContext.Items.Remove(existingItem);
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
            var imageBytes = memoryStream.ToArray();

            var scannedItem = await _geminiService.GetContentFromImage(imageBytes, image.ContentType);
            if (scannedItem == null) return BadRequest("Failed to scan item.");

            var imageUrl = await SaveImageToDiskAsync(image, imageBytes);

            scannedItem.Id = Guid.NewGuid().ToString();
            scannedItem.Count = 1;
            scannedItem.ImageUrl = imageUrl;

            return Ok(MapToResponseDto(scannedItem));
        }

        [HttpPost("rescan")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RescanItemAsync([FromForm] RescanUploadRequest request)
        {
            var image = request.Image;
            if (image == null || image.Length == 0) return BadRequest("No image uploaded.");

            var itemOld = System.Text.Json.JsonSerializer.Deserialize<ScannedItem>(
                request.ItemOldJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            if (itemOld == null) return BadRequest("Invalid old item data.");

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var scannedItem = await _geminiService.GetReprocessedContentFromImage(imageBytes, image.ContentType, itemOld);
            if (scannedItem == null) return BadRequest("Failed to scan item.");

            var imageUrl = await SaveImageToDiskAsync(image, imageBytes);

            scannedItem.Id = Guid.NewGuid().ToString();
            scannedItem.Count = 1;
            scannedItem.ImageUrl = imageUrl;

            return Ok(MapToResponseDto(scannedItem));
        }

        private async Task<string> SaveImageToDiskAsync(IFormFile image, byte[] imageBytes)
        {

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/uploads/{uniqueFileName}";
        }

        private static ItemResponseDto MapToResponseDto(ScannedItem item)
        {
            return new ItemResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Type = item.Type.ToString(),
                Count = item.Count,
                ImageUrl = item.ImageUrl
            };
        }

        public class ImageUploadRequest
        {
            public IFormFile Image { get; set; } = null!;
        }

        public class RescanUploadRequest
        {
            public IFormFile Image { get; set; } = null!;
            public string ItemOldJson { get; set; } = string.Empty;
        }
    }
}