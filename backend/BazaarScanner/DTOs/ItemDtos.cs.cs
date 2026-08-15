using BazaarScanner.Models;

namespace BazaarScanner.DTOs
{
    public class ItemResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateItemDto
    {
        public string Name { get; set; } = string.Empty;
        public ScannedItemType Type { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateItemDto
    {
        public string Name { get; set; } = string.Empty;
        public ScannedItemType Type { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
    }
}