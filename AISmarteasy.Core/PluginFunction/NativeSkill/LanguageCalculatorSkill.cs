using System.ComponentModel;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Context;
using NCalc;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


public class LanguageCalculatorSkill
{
    [SKFunction, Description("Useful for getting the result of a non-trivial math expression.")]
    public string Calculate(
        [Description("A valid mathematical expression that could be executed by a calculator capable of more advanced math functions like sin/cosine/floor.")]
        string input, 
        SKContext context)
    {
        string answer;

        string pattern = @"```\s*(.*?)\s*```";

        Match match = Regex.Match(input, pattern, RegexOptions.Singleline);
        if (match.Success)
        {
            return EvaluateMathExpression(match);
        }

        throw new InvalidOperationException($"Input value [{input}] could not be understood");
    }

    private static string EvaluateMathExpression(Match match)
    {
        var textExpressions = match.Groups[1].Value;
        var expr = new Expression(textExpressions, EvaluateOptions.IgnoreCase);
        expr.EvaluateParameter += delegate (string name, ParameterArgs args)
        {
            args.Result = name.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                _ => args.Result
            };
        };

        try
        {
            if (expr.HasErrors())
            {
                return "Error:" + expr.Error + " could not evaluate " + textExpressions;
            }

            var result = expr.Evaluate();
            return string.Empty + result;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("could not evaluate " + textExpressions, e);
        }
    }
}
