using BazaarScanner.Models;
using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Json.Nodes;

namespace BazaarScanner.Services
{
    public class GeminiService
    {
        private readonly Client _client;

        public GeminiService(IConfiguration configuration)
        {
            var apiKey = configuration["GeminiApi:ApiKey"] ?? throw new ArgumentNullException("Gemini API key not found!");
            _client = new Client(apiKey: apiKey);
        }

        public async Task<ScannedItem?> GetContentFromImage(byte[] imageBytes, string mimeType)
        {
            var contents = new List<Content>
            {
                new() {
                    Parts =
                    [
                        new Part { Text = "Analyzuj tento předmět z půdy a vytvoř pro něj prodejní popisek a zařazení." },
                        Part.FromBytes(imageBytes, mimeType)
                    ]
                }
            };

            string schemaString = @"
            {
              ""type"": ""object"",
              ""properties"": {
                ""Name"": { ""type"": ""string"", ""description"": ""Name of product from image"" },
                ""Type"": { 
                    ""type"": ""string"", 
                    ""enum"": [""Other"", ""Electronic"", ""Book"", ""Clothing"", ""Toy"", ""Media""] 
                }
              },
              ""required"": [""Name"", ""Type""]
            }";

            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-3.5-flash",
                contents: contents,
                config: new GenerateContentConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = JsonNode.Parse(schemaString)
                }
            );

            string text = response.Candidates[0].Content.Parts[0].Text;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var item = System.Text.Json.JsonSerializer.Deserialize<ScannedItem>(text, options);

            return item;
        }
    }
}