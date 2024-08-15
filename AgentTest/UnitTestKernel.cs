
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

}

