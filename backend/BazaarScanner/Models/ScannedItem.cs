namespace BazaarScanner.Models
{
    public enum ScannedItemType
    {
        Other,
        Electronic,
        Book,
        Clothing,
        Toy,
        Media
    }

    public class ScannedItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ScannedItemType Type { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
    }
}