
using Microsoft.Extensions.Configuration;
using KernelAgent;

namespace KernelTest;

public class UnitTestKernel : IDisposable
{
    Agent agent;
    public string imagesRootPath = "";
    public UnitTestKernel()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<UnitTestKernel>().Build();

        var apiKey = config["OPENAI_API_KEY"];

        if (apiKey == null)
        {
            throw new NullReferenceException("Api Key not define");
        }

        imagesRootPath = config["IMAGES_ROOT_PATH"] ?? "";

        if (string.IsNullOrEmpty(imagesRootPath))
        {
            throw new NullReferenceException("Define the path of the images");
        }

        agent = new Agent(apiKey);
    }

    public void Dispose()
    {
        Console.WriteLine("some data");
    }

    [Fact]
    public void TestAddFunction()
    {
        string password = "random997";
        var math = new MathPlugin();
        int sum = math.AddDigits(password);
        Assert.Equal(25, sum);

        password = "random0";
        sum = math.AddDigits(password);
        Assert.Equal(0, sum);

        password = "random";
        sum = math.AddDigits(password);
        Assert.Equal(0, sum);

        password = "r1a1n1d1o1m1";
        sum = math.AddDigits(password);
        Assert.Equal(6, sum);
    }


    [Fact]
    public void TestMultiplicationFunction()
    {

        string password = "ranVdom997V";
        var math = new MathPlugin();
        int mul = math.MultiplyRomanNumerals(password);
        Assert.Equal(25, mul);

        password = "randIjom0V";
        mul = math.MultiplyRomanNumerals(password);
        Assert.Equal(5, mul);

        password = "randjom0";
        mul = math.MultiplyRomanNumerals(password);
        Assert.Equal(0, mul);

        password = "randIVjom0VIIdasdX";
        mul = math.MultiplyRomanNumerals(password);
        Assert.Equal(280, mul);
    }


    [Fact]
    public async Task TestSendImage()
    {
        string ImageFilePath = $"{this.imagesRootPath}/screenshot.png";
        var response = await agent.GetTextFromImageSK(ImageFilePath);
        Console.WriteLine(response);
        Assert.Equal("some password", response);
    }

    [Fact]
    public async Task TestAskQ()
    {
        var chat = agent.GetChat();
        var response = await agent.AskQ(chat, "Can you add the digits of the password 'has3j2h33321' using the function add_password_digits");
        Console.WriteLine(response);
        Assert.Equal("17", response);
    }
}

