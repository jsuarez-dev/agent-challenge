﻿

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
            Temperature = 0
        };

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        // Web Access

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");

        string ImageFilePath = $"{secrets.API_KEY}screenshot.png";

        string password = "monkeY1@";


        for (int i = 0; i < 5; i++)
        {
            var listOfRulesAchived = new List<string>();
            var listOfRulesNoAchived = new List<string>();
            await page.Locator(".ProseMirror").FillAsync(password);
            foreach (var rule in await page.Locator("div.rule").AllAsync())
            {
                var names = await rule.EvaluateAsync("node => node.className");

                if (names.ToString().IndexOf("rule-error") != -1)
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync();
                    listOfRulesNoAchived.Add($"- {ruleText}");
                }
                else
                {
                    string ruleText = await rule.Locator("div.rule-desc").TextContentAsync();
                    listOfRulesAchived.Add($"- {ruleText}");
                }
            }
            Thread.Sleep(2000);

            //password = await agent.GeneratePassword(password, listOfRulesAchived, listOfRulesNoAchived);



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


            ChatHistory verificaton = InitializeVerificationPrompt();
            bool ValidPassword = false;
            int ReTries = 0;

            while (!ValidPassword && ReTries < 3) 
            {
                var response = await chat.GetChatMessageContentAsync(
                        history,
                        executionSettings: settings,
                        kernel: kernel
                        );

                password = response.Content ?? "";

                string PromptVerification = $"""
                    Verfied if the password: {password}
                    satisfied the rules:
                    {JoinRules(listOfRulesNoAchived, listOfRulesAchived)}

                    ONLY RESPOND TRUE OR FALSE
                    """;

                verificaton.AddUserMessage(PromptVerification);
                Thread.Sleep(100);

                response = await chat.GetChatMessageContentAsync(
                        verificaton,
                        executionSettings: settings,
                        kernel: kernel
                        );

                ValidPassword = bool.Parse( response.Content ?? "Error");
                ReTries++;
            }


        }

    }

    public static string JoinRules(List<string> noAchivedRules, List<string> achivedRules)
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
        return sbMessage.ToString();
    }


    public static (string API_KEY, string IMAGES_ROOT_PATH) GetSecrets()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var API_KEY= config["OPENAI_API_KEY"];

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
    public static ChatHistory InitializeVerificationPrompt()
    {
        const string systemPrompt = """
        You are a helpfull assistand that verified passwords
        
        ----------
        Exmaple 1 :

        Requirements:
        (1) Your password must be at least 5 characters.
        (2) Your password must include a special character.
        (3) Your password must include a number.
        (4) Your password must include an uppercase letter.
        (5) The digits in your password must add up to 15.

        Password: Fwils!537
        
        - "Fwils!537" does satisfied the requiement (1) because is 9 caracters long.
        - "Fwils!537" does satisfied the requiement (2) because inclued the special caracter "!" .
        - "Fwils!537" does satisfied the requiement (3) because inclued 3 numbers "537".
        - "Fwils!537" does satisfied the requiement (4) because "F" is capital letter.
        - "Fwils!537" does satisfied the requiement (5) because the digits "537" add up to 15, in other words 5+3+7=15.

        The result is TRUE

        ----------
        Exmaple 2 :

        Requirements:
        (1) Your password must be at least 5 characters.
        (2) Your password must include a special character.
        (3) Your password must include a number.
        (4) Your password must include an uppercase letter.
        (5) The digits in your password must add up to 25.

        Password: Fwils!537
        
        - "Fwils537" does satisfied the requiement (1) because is 9 caracters long.
        - "Fwils537" does not satisfied the requiement (2) because does not include any special caracter .
        - "Fwils537" does satisfied the requiement (3) because inclued 3 numbers "537".
        - "Fwils537" does satisfied the requiement (4) because "F" is capital letter.
        - "Fwils537" does not satisfied the requiement (5) because the digits "537" does not add up to 25, in other words 5+3+7=15 != 25.

        The result is FALSE

        """;
        var chat = new ChatHistory(systemPrompt);

        return chat;
    }
}
