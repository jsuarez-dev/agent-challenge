

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

        var logger = loggerFactory.CreateLogger("Agent");

        // Get Secrets
        var secrets = GetSecrets();

        // Kernel Configuration

        Kernel kernel = GetKernel(secrets.API_KEY, loggerFactory);


        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0.5
        };

        var chat = kernel.GetRequiredService<IChatCompletionService>();

        // Web Access

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");


        string password = "monkey";

        for (int i = 0; i < 10; i++)
        {
            ChatHistory history = InitializePrompt();

            var ListOfRulesAchived = new StringBuilder();
            var ListOfRulesNoAchived = new StringBuilder();
            await page.Locator(".ProseMirror").FillAsync(password);
            int ruleId = 1;
            foreach (var rule in await page.Locator("div.rule").AllAsync())
            {
                var names = await rule.EvaluateAsync("node => node.className");

                if (names?.ToString().IndexOf("rule-error") != -1)
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync() ?? "";
                    ListOfRulesNoAchived.AppendLine($"({ruleId}) {ruleText}.");
                }
                else
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync() ?? "";
                    ListOfRulesAchived.AppendLine($"({ruleId}) {ruleText}.");
                }
                ruleId ++;
            }

            Thread.Sleep(1000);

            string UserInput = $"""
                you are a agent which generate a improve Passwords

                you suggested this password '{password}', that password satisfied these rules:

                {ListOfRulesAchived.ToString()}

                But not these rules:

                {ListOfRulesAchived.ToString()}

                base on the rules can you change the password to achieve all the requirements
                ONLY RESPOND WITH THE PASSWORD
                """;

            history.AddUserMessage(UserInput);

            var response = await chat.GetChatMessageContentAsync(
                    history,
                    executionSettings: settings,
                    kernel: kernel
                    );

            password = response.Content ?? "";
            logger.LogInformation($"Password : {password}");
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
        string systemPrompt = """
            you are a friendly assistant that helps to pass the password game

            ---------------------------------------------------------
            Example:

            you are an agent which generates improved passwords

            you suggested this password 'Fwils5', that password satisfied these rules:

                (1) Your password must be at least 5 characters.
                (2) Your password must include a special character.
                (3) Your password must include a number.
                (4) Your password must include an uppercase letter.

            But not these rules:

                (5) The digits in your password must add up to 15.

            based on the rules can you change the password to achieve all the requirements
            ONLY RESPOND WITH THE PASSWORD

            - To satisfy the requirement (1) you could start with five random characters like: fwils
            - To satisfy the requirement (2) you add a special character at the end like: fwils!
            - To satisfy the requirement (3) you add a random number between 0-9 at the end like: fwils!5
            - To satisfy the requirement (4) you change any character like: Fwils! 
            - To satisfy the requirement (5) you could use the 5 that is already in it and add 7 and 3, because "5+3+7=15" and will end like: Fwils!537
            
            Response:
            
            Fwils!537

            ---------------------------------------------------------
        """;

        var chat = new ChatHistory(systemPrompt);

        return chat;
    }
}
