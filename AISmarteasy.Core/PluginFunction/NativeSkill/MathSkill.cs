using System.ComponentModel;
using System.Globalization;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


public class MathSkill
{
    [SKFunction, Description("Take the square root of a number")]
    public string Sqrt(string input)
    {
        var result = Convert.ToDouble(input, CultureInfo.InvariantCulture);
        result = System.Math.Sqrt(result);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    [SKFunction, Description("Add two numbers")]
    [SKParameter("input", "The first number to add")]
    [SKParameter("number", "The second number to add")]
    public string Add(SKContext context)
    {
        var result = Convert.ToDouble(context.Variables["input"], CultureInfo.InvariantCulture) + Convert.ToDouble(context.Variables["number"], CultureInfo.InvariantCulture);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    [SKFunction, Description("Subtract two numbers")]
    [SKParameter("input", "The first number to subtract from")]
    [SKParameter("number", "The second number to subtract away")]
    public string Subtract(SKContext context)
    {
        var result = Convert.ToDouble(context.Variables["input"], CultureInfo.InvariantCulture) - Convert.ToDouble(context.Variables["number"], CultureInfo.InvariantCulture);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    [SKFunction, Description("Multiply two numbers. When increasing by a percentage, don't forget to add 1 to the percentage.")]
    [SKParameter("input", "The first number to multiply")]
    [SKParameter("number", "The second number to multiply")]
    public string Multiply(SKContext context)
    {
        return (
            Convert.ToDouble(context.Variables["input"], CultureInfo.InvariantCulture) *
            Convert.ToDouble(context.Variables["number"], CultureInfo.InvariantCulture)
        ).ToString(CultureInfo.InvariantCulture);
    }

    [SKFunction, Description("Divide two numbers")]
    [SKParameter("input", "The first number to divide from")]
    [SKParameter("number", "The second number to divide by")]
    public string Divide(SKContext context)
    {
        return (
            Convert.ToDouble(context.Variables["input"], CultureInfo.InvariantCulture) /
            Convert.ToDouble(context.Variables["number"], CultureInfo.InvariantCulture)
        ).ToString(CultureInfo.InvariantCulture);
    }
}
