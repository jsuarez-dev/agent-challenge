using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.RegularExpressions;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace KernelAgent;

public class Agent
{

    private string _apiKey { set; get; }
    private const LogLevel MinLogLevel = LogLevel.Trace;

    private ChatHistory chatHistory;

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


    public Agent(string apiKey)
    {
        this._apiKey = apiKey;

        this.chatHistory = new ChatHistory();
    }

    public async Task<string> GeneratePassword(string password, List<string> achivedRules, List<string> noAchivedRules)
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

        //

        const string example = """
        You are a helpfull assistand that generate passwords
        
        Exmaple:

        Requirements:
        (1) Your password must be at least 5 characters.
        (2) Your password must include a special character.
        (3) Your password must include a number.
        (4) Your password must include an uppercase letter.
        (5) The digits in your password must add up to 15.
        
        - To satisfied the requiement (1) you could start with five ramdon characters like: fwils
        
        - To satisfied the requiement (2) you add an special caracter at the end like: fwils!

        - To satisfied the requiement (3) you add a ramdon number between 0-9 at the end like: fwils!5

        - To satisfied the requiement (4) you change any character like: Fwils!
        
        - To satisfied the requiement (5) you could use the 5 that is already in it and add 7 and 3, because "5+3+7=15" and will end like: Fwils!537

        Result

        Fwils!537

        """;

        this.chatHistory.Add(
                new()
                {
                    Role = AuthorRole.System,
                    Content = example
                });

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

        this.chatHistory.Add(
                new()
                {
                    Role = AuthorRole.User,
                    Content = sbMessage.ToString()
                });

        var kernel = CreateKernelAgent(loggerFactory);

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(
                this.chatHistory,
                kernel: kernel,
                executionSettings: openAIPromptExecutionSettings
                );


        if (response.Content != null)
        {
            return response.Content;
        }
        return "";

    }

    public async Task<string> AskQ(string question)
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

        var chat = new ChatHistory
        {
            new()
            {
                Role = AuthorRole.User,
                Content = question
            }
        };

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

public class MathPlugin
{
    [KernelFunction("add_password_digits")]
    [Description("This function take a password and add all the digits on it")]
    [return: Description("return an int value that represent the sum of all the digits of the password")]
    public int AddDigits(string password)
    {
        int sum = 0; 
        foreach (char c in password)
        {
            if (char.IsDigit(c))
            {
                sum += (int)char.GetNumericValue(c);
            }
        }
        return sum;
    }
    [KernelFunction("multiply_roman_numerals")]
    [Description("This function take a password and multiply all roman numerals")]
    [return: Description("return an int value that represent the multiplication of all roman numerals of the password")]
    public int MultiplyRomanNumerals(string password)
    {
        string pattern = @"[MCDXLIV]+";
        Regex regex = new Regex(pattern);
        MatchCollection matches = regex.Matches(password);

        if (matches.Count > 0)
        {
            int romanNumerals = 1;
            foreach (Match match in matches)
            {
                int value = this.RomanToInt(match.Value);
                romanNumerals *= value;
            }

            return romanNumerals;
        }
        return 0;
    }

    private int RomanToInt(string roman)
    {
        Dictionary<char, int> romanValues = new Dictionary<char, int>
        {
            {'I', 1},
            {'V', 5},
            {'X', 10},
            {'L', 50},
            {'C', 100},
            {'D', 500},
            {'M', 1000}
        };

        int total = 0;
        int prevValue = 0;

        for (int i = roman.Length - 1; i >= 0; i--)
        {
            int value = romanValues[roman[i]];
            if (value < prevValue)
            {
                total -= value;
            }
            else
            {
                total += value;
            }
            prevValue = value;
        }

        return total;
    }

}
