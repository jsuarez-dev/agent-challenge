
using Microsoft.Extensions.Configuration;
using WebConnection;

namespace KernelTest;

public class UnitTestKernel : IDisposable
{
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

    }

    public void Dispose()
    {
        Console.WriteLine("some data");
    }

    [Fact]
    public void TestAddFunction()
    {
        string password = "random997";
        var math = new Plugins();
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
        var math = new Plugins();
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
    public void TestDigitGeneration()
    {
        
        var math = new Plugins();
        string digits = math.GenerateDigits(25);
        Assert.True(math.CheckPasswordDigits(digits, 25));
        Assert.Equal(25, math.AddDigits(digits));

        digits = math.GenerateDigits(35);
        Assert.False(math.CheckPasswordDigits(digits, 25));
        Assert.Equal(35, math.AddDigits(digits));
    }

    [Fact]
    public void TestRomanNumberGeneration()
    {
        
        var math = new Plugins();
        string digits = math.GenerateRomanNumbers(25);
        Assert.Equal(25, math.MultiplyRomanNumerals(digits));

        digits = math.GenerateRomanNumbers(35);
        Assert.Equal(35, math.MultiplyRomanNumerals(digits));
    }
}

