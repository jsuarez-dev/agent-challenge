

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
        // Open Telemetry
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

        // Get Secrets
        var secrets = GetSecrets();

        // Kernel Configuration

        Kernel kernel = GetKernel(secrets.API_KEY, loggerFactory);


        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0.7
        };

        var chat = kernel.GetRequiredService<IChatCompletionService>();

        // Web Access

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");


        string password = "monkey";

        ChatHistory history = InitializePrompt();

        for (int i = 0; i < 8; i++)
        {

            var ListOfRulesAchived = new StringBuilder();
            var ListOfRulesNoAchived = new StringBuilder();
            await page.Locator(".ProseMirror").FillAsync(password);
            foreach (var rule in await page.Locator("div.rule").AllAsync())
            {
                var names = await rule.EvaluateAsync("node => node.className");

                if (names?.ToString().IndexOf("rule-error") != -1)
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync() ?? "";
                    ListOfRulesNoAchived.AppendLine($"- {ruleText}");
                }
                else
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync() ?? "";
                    ListOfRulesAchived.AppendLine($"- {ruleText}");
                }
            }

            Thread.Sleep(2000);

            string UserInput = $"""
                you are a agent which generate a improve Passwords

                you suggested this password '{password}', that password satisfied this rules:

                {ListOfRulesAchived.ToString()}

                But not this rules:

                {ListOfRulesAchived.ToString()}

                base on the image can you suggest what to change on the password to achieve all the requirements
                ONLY RESPOND WITH THE PASSWORD
                """;


            history.AddUserMessage(UserInput);


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

        var API_KEY = config["OPEN_AI_API_KEY"];

        if (API_KEY == null)
        {
            throw new NullReferenceException("Api Key not define");
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

        return builder.Build();
    }

    public static ChatHistory InitializePrompt()
    {
        string systemPrompt = "you are a friendly assistant that help to pass the password game";

        var chat = new ChatHistory(systemPrompt);

        return chat;
    }
}
