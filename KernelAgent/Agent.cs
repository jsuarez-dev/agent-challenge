
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace KernelAgent;

public class Agent
{

    Kernel kernel;
    ChatHistory chatHistory;
    IChatCompletionService chatCompletionService;

    private const LogLevel MinLogLevel = LogLevel.Trace;


    public Agent(string apiKey)
    {
        var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("TelemetryExample");

        using var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddSource("Telemetry.Example")
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .Build();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("Microsoft.SemanticKernel*")
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            // Add OpenTelemetry as a logging provider
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
                // Format log messages. This is default to false.
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });
            builder.SetMinimumLevel(MinLogLevel);
        });



        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(loggerFactory);

        builder.Services.AddOpenAIChatCompletion("gpt-4", apiKey);

        //KernelPlugin mathPlugin = KernelPluginFactory.CreateFromType<MathPlugin>("Math");
        builder.Services.AddSingleton<MyMathPlugin>();

        builder.Plugins.AddFromObject(new MyMathPlugin());

        kernel = builder.Build();

        chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();


        const string example = """
        You are a helpfull assistand that generate passwords
        
        Exmaple:

        Rules:
        - Your password must be at least 5 characters.
        - Your password must include a special character.
        - Your password must include a number.
        - Your password must include an uppercase letter.
        - The digits in your password must add up to 15.

        Password: Random!528

        """;

        chatHistory = new ChatHistory();
        chatHistory.Add(
                new()
                {
                    Role = AuthorRole.System,
                    Content = example
                });


    }

    public async Task<string> GeneratePassword(string password, List<string> achivedRules, List<string> noAchivedRules)
    {
        StringBuilder sbMessage = new StringBuilder();

        sbMessage.AppendLine("Rules:");
        foreach (var rule in achivedRules)
        {
            sbMessage.AppendLine(rule);
        }
        foreach (var rule in noAchivedRules)
        {
            sbMessage.AppendLine(rule);

        }

        sbMessage.AppendLine("Only anwser the password");
        sbMessage.AppendLine("Password:");

        chatHistory.Add(
                new()
                {
                    Role = AuthorRole.User,
                    Content = sbMessage.ToString()
                });


        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var responce = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                kernel: kernel
                //executionSettings: openAIPromptExecutionSettings
                );


        return responce.ToString();
    }

    public async Task<string> AskQ(string question)
    {

        chatHistory.Add(
                new()
                {
                    Role = AuthorRole.User,
                    Content = question
                });


        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var responce = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel
                );


        return responce.ToString();
    }
}

public class MyMathPlugin
{

    [KernelFunction, Description("Add two numbers")]
    public double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2
    )
    {
        return number1 + number2;
    }

}
