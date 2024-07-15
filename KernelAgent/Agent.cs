

using System.Text;
using System.Reflection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ImageToText;
using Microsoft.SemanticKernel.ChatCompletion;


#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace KernelAgent;

public class Agent
{

    Kernel kernel;
    KernelFunction kernelFunction;
    ImageApi imageApi;
    IChatCompletionService chatCompletion;

    public Agent(string apiKey)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4o", apiKey);

        kernel = builder.Build();

        var prompt = @"
        you are a agent which generate a improve Passwords
           
        If the rules ask for 'digits' means you will need to give a 0-9.
        If the rules ask for digits add 12, you could suggest 66 or 93 or 156.
        
        you suggested this password '{{$Password}}', that password satisfied this rules:
        
        {{$AchivedRules}}

        But not this rules:
           
        {{$NoAchivedRules}}

        Make the minimal changes to the suggested password to achieve all rules 

        Only respond with the password.
        ";

        kernelFunction = kernel.CreateFunctionFromPrompt(prompt);

        this.chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        this.imageApi = new ImageApi(apiKey);
    }

    public async Task<string> GeneratePassword(string password, List<string> achivedRules, List<string> noAchivedRules)
    {
        StringBuilder sbAchived = new StringBuilder();
        foreach (var rule in achivedRules)
        {
            sbAchived.AppendLine(rule);
        }

        StringBuilder sbNoAchived = new StringBuilder();
        foreach (var rule in noAchivedRules)
        {
            sbNoAchived.AppendLine(rule);
        }

        FunctionResult functionResult = await kernel.InvokeAsync(
            kernelFunction,
            new()
            {
                ["AchivedRules"] = sbAchived.ToString(),
                ["NoAchivedRules"] = sbNoAchived.ToString(),
                ["Password"] = password
            });

        string result = functionResult.ToString();
        Console.WriteLine(functionResult.RenderedPrompt);

        return result;
    }


    public async Task<string> GetTextFromImage(string ImageFilePath)
    {
        string responce = await this.imageApi.SendImage(ImageFilePath);
        Console.WriteLine(responce);
        return responce;
    }

    public async Task<string> GetTextFromImageSK(string ImageFilePath)
    {
        string systemPrompt = "you are a friendly assistant that helps describe images.";
        string userInput = "What is this image?";

        var imageBytes = File.ReadAllBytes(ImageFilePath);

        var chatHistory = new ChatHistory(systemPrompt);
        var base64_image = Convert.ToBase64String(imageBytes);

        chatHistory.AddUserMessage(
                [
                new TextContent(userInput),
                new ImageContent(new Uri($"data:image/jpeg;base64,{base64_image}")),
                ]);

        var reply = await chatCompletion.GetChatMessageContentAsync(chatHistory);

        return reply.Content;
    }
}
