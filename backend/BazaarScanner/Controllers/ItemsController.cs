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

        public ItemsController(GeminiService geminiService, AppDbContext appDbContext)
        {
            _geminiService = geminiService;
            _appDbContext = appDbContext;
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

            var responseDto = MapToResponseDto(newItem);
            return CreatedAtAction(nameof(GetAllItems), new { id = newItem.Id }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(string id, [FromBody] UpdateItemDto dto)
        {
            var existingItem = await _appDbContext.Items.FindAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = dto.Name;
            existingItem.Type = dto.Type;
            existingItem.Count = dto.Count;
            existingItem.ImageUrl = dto.ImageUrl;

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

            var scannedItem = await _geminiService.GetReprocessedContentFromImage(memoryStream.ToArray(), image.ContentType, itemOld);
            if (scannedItem == null) return BadRequest("Failed to scan item.");

            scannedItem.Id = Guid.NewGuid().ToString();
            scannedItem.Count = 1;

            return Ok(MapToResponseDto(scannedItem));
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