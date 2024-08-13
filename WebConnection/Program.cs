

using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Text;


namespace WebConnection;

class Program
{
    static async Task Main()
    {
        var secrets = GetSecrets();
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
            builder.SetMinimumLevel(LogLevel.Trace);

        });

        // Kernel Configuration

        Kernel kernel = GetKernel(secrets.API_KEY, loggerFactory);


        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7
        };

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        // Web Access

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");

        string ImageFilePath = $"{secrets.IMAGES_ROOT_PATH}screenshot.png";

        string password = "monkeY3@";


        for (int i = 0; i < 10; i++)
        {
            var listOfRulesAchived = new List<string>();
            var listOfRulesNoAchived = new List<string>();
            await page.Locator(".ProseMirror").FillAsync(password);



            Thread.Sleep(2000);

            await page.ScreenshotAsync(new()
            {
                Path = ImageFilePath,
                FullPage = true,
            });

            string userInput = """
                base on the image can you suggest what to change on the password to achive all the requirements
                ONLY RESPOND WITH THE PASSWORD
                """;

            var imageBytes = await File.ReadAllBytesAsync(ImageFilePath);


            ChatHistory history = InitializePrompt();

            history.AddUserMessage(
                    [
                    new TextContent(userInput),
                    new ImageContent(imageBytes, "image/png")
                    ]);


            var response = await chat.GetChatMessageContentAsync(
                    history,
                    executionSettings: settings,
                    kernel: kernel
                    );

            password = response.Content ?? "";



        }

    }


    public static (string API_KEY, string IMAGES_ROOT_PATH) GetSecrets()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var API_KEY = config["OPENAI_API_KEY"];

        if (API_KEY == null)
        {
            throw new NullReferenceException("Apu Key not define");
        }
        var IMAGES_ROOT_PATH = config["IMAGES_ROOT_PATH"] ?? "";

        if (string.IsNullOrEmpty(IMAGES_ROOT_PATH))
        {
            throw new NullReferenceException("Define the path of the images");
        }

        return (API_KEY, IMAGES_ROOT_PATH);

    }
    public static Kernel GetKernel(string API_KEY, ILoggerFactory? loggerFactory = null)
    {

        var builder = Kernel.CreateBuilder();

        if (loggerFactory != null)
        {
            builder.Services.AddSingleton(loggerFactory);
        }


        builder.Services.AddOpenAIChatCompletion("gpt-4o", API_KEY);

        builder.Services.AddSingleton<Plugins>();

        builder.Plugins.AddFromObject(new Plugins());

        return builder.Build();
    }

    public static ChatHistory InitializePrompt()
    {
        string systemPrompt = "you are a friendly assistant that help to pass the password game";

        var chat = new ChatHistory(systemPrompt);

        return chat;
    }
}
