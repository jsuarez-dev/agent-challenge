
using Microsoft.Extensions.Configuration;
using KernelAgent;

namespace KernelTest;

public class UnitTestKernel : IDisposable
{
    Agent agent;
    public UnitTestKernel()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<UnitTestKernel>().Build();

        var apiKey = config["OPENAI_API_KEY"];

        if (apiKey == null)
        {
            throw new NullReferenceException("Api Key not define");
        }

        agent = new Agent(apiKey);
    }

    public void Dispose()
    {
        Console.WriteLine("some data");
    }

    [Fact]
    public async Task Test1()
    {
        string password = "random";
        List<string> rulesPass = [""];
        List<string> rulesNotPass = [
        "- Your password must be at least 5 characters.",
        "- Your password must include a special character.",
        "- Your password must include a number.",
        "- Your password must include an uppercase letter.",
        "- The digits in your password must add up to 25."];
        var response = await this.agent.GeneratePassword(password, rulesPass, rulesNotPass);
        Console.WriteLine(response);

        Assert.Equal("some password", response);
    }

    [Fact]
    public async Task TestQ()
    {

        var response = await this.agent.AskQ("can you add 456789 plus 54654654");
        Console.WriteLine(response);

        int total = 456789 + 54654654;
        Assert.Equal(total.ToString(), response);
    }

}

