using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;


namespace KernelAgent;

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
}
