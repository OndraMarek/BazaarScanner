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
                        new Part { Text = "Analyze the item in the image and identify it as precisely as possible. " +
                        "Provide a concise, factual name in that I can use as a search query to find this exact or similar items online. " +
                        "Then, categorize it strictly according to the provided schema." },
                        Part.FromBytes(imageBytes, mimeType)
                    ]
                }
            };

            string schemaString = GetSchemaString();

            var response = await GenerateContentAsync(contents, schemaString);

            return GetScannedItemFromResponse(response);
        }

        public async Task<ScannedItem?> GetReprocessedContentFromImage(byte[] imageBytes, string mimeType, ScannedItem itemOld)
        {
            var contents = new List<Content>
            {
                new() {
                    Parts =
                    [
                        new Part {
                            Text = "Analyze the item in the image again. The attached JSON contains a previous identification attempt that was incorrect, inaccurate, or too generic. " +
                                   "Please review the image carefully, avoid repeating the previous mistake, and identify the item as precisely as possible. " +
                                   "Provide a new, concise, factual name that I can use as a search query to find exact or similar items online. " +
                                   "Then, categorize it strictly according to the provided schema."
                        },
                        new Part { Text = "Previous incorrect identification: " + System.Text.Json.JsonSerializer.Serialize(itemOld) },
                        Part.FromBytes(imageBytes, mimeType)
                    ]
                }
            };

            string schemaString = GetSchemaString();

            var response = await GenerateContentAsync(contents, schemaString);

            return GetScannedItemFromResponse(response);
        }

        private string GetSchemaString()
        {
            return @"
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
        }

        private async Task<GenerateContentResponse> GenerateContentAsync(List<Content> contents, string schemaString)
        {
            int maxRetries = 3;
            int delayMilliseconds = 2000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await _client.Models.GenerateContentAsync(
                        model: "gemini-3.5-flash",
                        contents: contents,
                        config: new GenerateContentConfig
                        {
                            ResponseMimeType = "application/json",
                            ResponseJsonSchema = JsonNode.Parse(schemaString)
                        }
                    );
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    Console.WriteLine($"[Attempt {i + 1}/{maxRetries} failed] API is overloaded: {ex.Message}. Trying again in {delayMilliseconds} ms...");

                    await Task.Delay(delayMilliseconds);

                    delayMilliseconds *= 2;
                }
            }

            throw new Exception("Service Gemini AI is currently too busy and did not respond even after several attempts. Please try again later.");
        }

        private ScannedItem? GetScannedItemFromResponse(GenerateContentResponse response)
        {
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