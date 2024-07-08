
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
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Console.WriteLine(functionResult.RenderedPrompt);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        return result;
    }
}
