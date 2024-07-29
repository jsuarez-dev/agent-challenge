﻿
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

        if (apiKey == null)
        {
            throw new NullReferenceException("Apu Key not define");
        }
        var imagesRootPath = config["IMAGES_ROOT_PATH"] ?? "";

        if (string.IsNullOrEmpty(imagesRootPath))
        {
            throw new NullReferenceException("Define the path of the images");
        }

        Agent agent = new Agent(apiKey);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://neal.fun/password-game/");

        string imageFilePath = $"{imagesRootPath}screenshot.png";

        string password = "monkey";


        for (int i = 0; i < 12; i++)
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
                Path = imageFilePath,
                FullPage = true,
            });

            password = await agent.GetTextFromImageSK(imageFilePath);

        }

    }
}
