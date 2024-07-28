using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;


namespace KernelAgent;

public class ImageApi
{
    private string ApiKey;

    public ImageApi(string ApiKey)
    {
        this.ApiKey = ApiKey;
    }


    public async Task<string> SendImage(string imagePath)
    {
        // Read the image file and convert it to a Base64 string
        string base64Image = ConvertImageToBase64(imagePath);

        // Send the Base64 string to the API
        return await SendImageToApi(base64Image);
    }

    string ConvertImageToBase64(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(imageBytes);
    }

    public async Task<string> SendImageToApi(string base64Image)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.ApiKey}");

            Payload payload = new Payload
            {
                Model = "gpt-4o",
                Messages = new List<Message>()
                {
                    new Message
                    {
                        Role = "user",
                        Content = new ArrayList()
                        {
                            new TextContentJson
                            {
                                Type = "text",
                                Text = "What's in this image"
                            },
                            new ImageContentJson
                            {
                                Type ="image_url",
                                ImageUrl = new ImageURL()
                                {
                                    URL = $"data:image/png;base64,{base64Image}"
                                }
                            }
                        }

                    }
                }

            };

            string StrPayload = JsonSerializer.Serialize(payload);

            var content = new StringContent(StrPayload, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Send the POST request
            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            string StrResponse = await response.Content.ReadAsStringAsync();

            return StrResponse;
        }
    }
}

struct Headers
{
    [JsonPropertyName("Content-Type")]
    public string ContentType { get; set; }
    public string Authorization { get; set; }
}


struct Payload
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }
}

struct Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("content")]
    public ArrayList Content { get; set; }
}

struct TextContentJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

struct ImageContentJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("image_url")]
    public ImageURL ImageUrl { get; set; }
}

struct ImageURL
{
    [JsonPropertyName("url")]
    public string URL { get; set; }
}
