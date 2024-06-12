
using System.Threading;
using Microsoft.Playwright;

namespace WebConnection;


class Program
{
    static async Task Main()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");

        List<string> rules = new List<string>();
        string password = GeneratePassword(rules);
        for (int i = 0; i < 2; i++)
        {
            await page.Locator(".ProseMirror").FillAsync(password);
            foreach (var rule in await page.Locator(".rule-error").AllAsync())
            {
                rules.Add(await rule.TextContentAsync());
            }
            Thread.Sleep(2000);
            password = GeneratePassword(rules);
        }
    }

    static string GeneratePassword(List<string> rules)
    {
        if (rules.Count == 0)
        {
            return "johan";
        }

        return "johan92"; 
    }
}