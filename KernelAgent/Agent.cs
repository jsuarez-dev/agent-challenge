
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

namespace KernelAgent;

public class Agent
{
    Kernel kernel;
    KernelFunction kernelFunction;

    public Agent(string apiKey)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-3.5-turbo", apiKey);
        
        kernel = builder.Build();

        var prompt = @"
        you are a agent which generate a Password
        
        The Password must follow the next rules:
        
        {{$rules}}

        Only respond with the password.

        ";

        kernelFunction = kernel.CreateFunctionFromPrompt(prompt);

    }

    public async Task<string> GeneratePassword(string password, List<string> rules)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var rule in rules)
        {
            sb.AppendLine($"- {rule}");
        }

        FunctionResult functionResult = await kernel.InvokeAsync(kernelFunction, new() { ["rules"] = sb.ToString() });
        string result = functionResult.ToString();

        return result;
    }
}
