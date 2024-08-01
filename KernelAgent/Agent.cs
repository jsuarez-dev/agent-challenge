using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace KernelAgent;

public class Agent
{

    private string _apiKey { set; get; }
    private const LogLevel MinLogLevel = LogLevel.Trace;
    public Kernel? kernel_p;

    public Agent(string apiKey)
    {
        this._apiKey = apiKey;

    }

    private Kernel CreateKernelAgent(ILoggerFactory? loggerFactory = null)
    {

        var builder = Kernel.CreateBuilder();

        if (loggerFactory != null)
        {
            builder.Services.AddSingleton(loggerFactory);
        }


        builder.Services.AddOpenAIChatCompletion("gpt-4o", this._apiKey);

        builder.Services.AddSingleton<MathPlugin>();

        builder.Plugins.AddFromObject(new MathPlugin());

        return builder.Build();
    }


    public IChatCompletionService GetChat()
    {
        var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("Agent Telemetry");

        using var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddSource("Telemetry.Agent")
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

        Kernel kernel = this.CreateKernelAgent(loggerFactory);
        this.kernel_p = kernel;

        return kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> AskQ(IChatCompletionService chatCompletion, string question)
    {

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
        var chat = new ChatHistory(question);

        var response = await chatCompletion.GetChatMessageContentAsync(
                chat,
                executionSettings: openAIPromptExecutionSettings,
                kernel: this.kernel_p
                );


        if (response.Content != null)
        {
            return response.Content;
        }
        return "";
    }



    public async Task<string> GetTextFromImageSK(string ImageFilePath)
    {
        var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("Agent Telemetry");

        using var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddSource("Telemetry.Agent")
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

        string systemPrompt = "you are a friendly assistant that help to pass the password game";
        string userInput = """
            base on the image can you suggest what to change on the password to achive all the requirements

            make sure that the password satisfied the requirement 

            ONLY RESPOND WITH THE PASSWORD
            """;

        var imageBytes = await File.ReadAllBytesAsync(ImageFilePath);

        var chat = new ChatHistory(systemPrompt);

        chat.AddUserMessage(
                [
                new TextContent(userInput),
                    new ImageContent(imageBytes, "image/png")
                ]);


        var kernel = CreateKernelAgent(loggerFactory);

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatCompletion.GetChatMessageContentAsync(
                chat,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel
                );


        if (response.Content != null)
        {
            return response.Content;
        }
        return "";
    }
}

