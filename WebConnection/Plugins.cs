
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace WebConnection;
public class Plugins
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
