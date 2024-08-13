

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

        await page.EvaluateAsync("""
                const container = document.getElementsByClassName('password-box');

                const label = document.createElement('label');
                label.textContent = 'ProseMirror';
                label.style.display = 'inline';
                label.style.backgroundColor = '#ffc7c7';
                label.style.fontSize = '30px';
                label.style.color = '#333';
                label.style.marginBottom = '5px'
                label.style.marginTop = '5px'
                label.style.border = 'solid 1px black'

                container[0].appendChild(label);                
            """);

        await page.Locator(".ProseMirror").FillAsync(password);
        for (int i = 0; i < 10; i++)
        {


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
            

            string NavigationPrompt = $"""
                base on the image generate a Javascript script that access to the element that contain the password and insert the password:{password}
                The ClassName of the element is on black square
                ONLY RESPOND WITH THE CODE
                """;


            ChatHistory NavigationHistory = InitializePromptToNavigate();

            NavigationHistory.AddUserMessage(
                    [
                    new TextContent(NavigationPrompt),
                    new ImageContent(imageBytes, "image/png")
                    ]);

            response = await chat.GetChatMessageContentAsync(
                    NavigationHistory,
                    executionSettings: settings,
                    kernel: kernel
                    );

            string script = CleanJsScript(response.Content ?? "");
            Console.WriteLine(script);
            await page.EvaluateAsync(script);
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
    public static ChatHistory InitializePromptToNavigate()
    {
        string systemPrompt = "You are assitant tha genereate JavaScript code to interact with a webside";

        var chat = new ChatHistory(systemPrompt);

        return chat;
    }
    static string CleanJsScript(string input)
    {
        StringBuilder result = new StringBuilder();
        string[] lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (!line.TrimStart().StartsWith("```"))
            {
                result.AppendLine(line.Trim());
            }
        }

        return result.ToString();
    }
}
