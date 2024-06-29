
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using KernelAgent;


namespace WebConnection;

class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var apiKey = config["OPENAI_API_KEY"];

        Agent agent = new Agent(apiKey);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");

        List<string> rules = new List<string>();
        string password = InitPassword();
        for (int i = 0; i < 10; i++)
        {
            await page.Locator(".ProseMirror").FillAsync(password);
            foreach (var rule in await page.Locator(".rule-desc").AllAsync())
            {
                string textRule = await rule.TextContentAsync();
                rules.Add(textRule);
            }
            Thread.Sleep(2000);
            password = await agent.GeneratePassword(password, rules);
        }
    }

    static string InitPassword()
    {
        return "johan";
    }
}