using System.ComponentModel;
using System.Globalization;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class TextSkill
{
    [SKFunction, Description("Trim whitespace from the start and end of a string.")]
    public string Trim(string input) => input.Trim();

    [SKFunction, Description("Trim whitespace from the start of a string.")]
    public string TrimStart(string input) => input.TrimStart();

    [SKFunction, Description("Trim whitespace from the end of a string.")]
    public string TrimEnd(string input) => input.TrimEnd();

    [SKFunction, Description("Convert a string to uppercase.")]
    public string Uppercase(string input, CultureInfo? cultureInfo = null) => input.ToUpper(cultureInfo);

    [SKFunction, Description("Convert a string to lowercase.")]
    public string Lowercase(string input, CultureInfo? cultureInfo = null) => input.ToLower(cultureInfo);

    [SKFunction, Description("Get the length of a string.")]
    public int Length(string input) => input?.Length ?? 0;

    [SKFunction, Description("Concat two strings into one.")]
    public string Concat(
        [Description("First input to concatenate with")] string input,
        [Description("Second input to concatenate with")] string input2) =>
        string.Concat(input, input2);

    [SKFunction, Description("Echo the input string. Useful for capturing plan input for use in multiple functions.")]
    public string Echo(
      [Description("Input string to echo.")] string text)
    {
        return text;
    }

    [SKFunction, Description("Append the day variable")]
    public string AppendDay(
        [Description("Text to append to")] string input,
        [Description("Value of the day to append")] string day) =>
        input + day;
}
