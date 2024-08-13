
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace WebConnection;
public class Plugins
{

    [KernelFunction("check_password_add_rule")]
    [Description("This function check if a PASSWORD add up to spesific VALUE on the rule")]
    [return: Description("return True if the PASSWORD add to the give VALUE, False other wise")]
    public bool CheckPasswordDigits(string password, int value)
    {
        if (password == null || value == 0)
        { return false; }
        else if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        int sum = this.AddDigits(password);
        if (value == sum)
        {
            return true;
        }
        return false;

    }

    [KernelFunction("add_password_digits")]
    [Description("This function take a PASSWORD and add all the digits on it")]
    [return: Description("return an int value that represent the sum of all the digits of the PASSWORD")]
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


    [KernelFunction("generate_digits")]
    [Description("This function generate random digits that add up to an spesific VALUE ")]
    [return: Description("return an string of digits that add to the give it VALUE")]
    public string GenerateDigits(int value)
    {
        Random random = new Random();
        StringBuilder sb = new StringBuilder();
        while (true)
        {
            if (value < 10)
            {
                sb.Append(value.ToString());
                break;
            }
            int i = random.Next(1, 10);
            value = value - i;
            sb.Append(i.ToString());
        }

        return sb.ToString();
    }


    [KernelFunction("generate_roman_numbers")]
    [Description("This function generate random Roman Numerals that multiply up to an spesific VALUE ")]
    [return: Description("return an string of Roman Numerals that mulptiply up to the give it VALUE")]
    public string GenerateRomanNumbers(int value)
    {
        Random random = new Random();
        StringBuilder sb = new StringBuilder();
        while (true)
        {
            if (value < 10)
            {
                sb.Append("-");
                sb.Append(this.IntToRoman(value));
                break;
            }
            int i = random.Next(1, 10);
            if (value % i == 0)
            {
                value = value / i;
                sb.Append("-");
                sb.Append(this.IntToRoman(i));
            }
        }

        return sb.ToString();
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

    private string IntToRoman(int num)
    {
        if (num < 1 || num > 3999)
            throw new ArgumentOutOfRangeException("Value must be in the range 1-3999.");

        var romanNumerals = new List<(int, string)>
        {
            (1000, "M"),
            (900, "CM"),
            (500, "D"),
            (400, "CD"),
            (100, "C"),
            (90, "XC"),
            (50, "L"),
            (40, "XL"),
            (10, "X"),
            (9, "IX"),
            (5, "V"),
            (4, "IV"),
            (1, "I")
        };

        var result = string.Empty;

        foreach (var (value, symbol) in romanNumerals)
        {
            while (num >= value)
            {
                result += symbol;
                num -= value;
            }
        }

        return result;
    }
}
