using System.Text.RegularExpressions;
using System.Xml;
using SemanticKernel.Context;

namespace SemanticKernel.Planner;

internal static class SequentialPlanParser
{
    internal const string GoalTag = "goal";
    internal const string SolutionTag = "plan";
    internal const string FunctionTag = "function.";
    internal const string SetContextVariableTag = "setContextVariable";
    internal const string AppendToResultTag = "appendToResult";

    internal static Plan ToPlanFromXml(this string xmlString, string goal, bool allowMissingFunctions = false)
    {
        XmlDocument xmlDoc = new();
        try
        {
            xmlDoc.LoadXml("<xml>" + xmlString + "</xml>");
        }
        catch (XmlException e)
        {
            Regex planRegex = new(@"<plan\b[^>]*>(.*?)</plan>", RegexOptions.Singleline);
            Match match = planRegex.Match(xmlString);

            if (!match.Success)
            {
                match = planRegex.Match($"{xmlString}</plan>");
            }

            if (match.Success)
            {
                string planXml = match.Value;

                try
                {
                    xmlDoc.LoadXml("<xml>" + planXml + "</xml>");
                }
                catch (XmlException ex)
                {
                    throw new SKException($"Failed to parse plan xml strings: '{xmlString}' or '{planXml}'", ex);
                }
            }
            else
            {
                throw new SKException($"Failed to parse plan xml string: '{xmlString}'", e);
            }
        }

        var solution = xmlDoc.GetElementsByTagName(SolutionTag);

        var plan = new Plan(goal);

        foreach (XmlNode solutionNode in solution)
        {
            foreach (XmlNode childNode in solutionNode.ChildNodes)
            {
                if (childNode.Name == "#text" || childNode.Name == "#comment")
                {
                    continue;
                }

                if (childNode.Name.StartsWith(FunctionTag, StringComparison.OrdinalIgnoreCase))
                {
                    var pluginFunctionName = childNode.Name.Split(FunctionTagArray, StringSplitOptions.None)[1];
                    GetFunctionCallbackNames(pluginFunctionName, out var pluginName, out var functionName);

                    if (!string.IsNullOrEmpty(functionName))
                    {
                        var pluginFunction = KernelProvider.Kernel.FindFunction(pluginName, functionName);

                        var planStep = new Plan(pluginFunction);

                        var functionVariables = new ContextVariables();
                        var functionOutputs = new List<string>();
                        var functionResults = new List<string>();

                        var view = pluginFunction.Describe();
                        foreach (var p in view.Parameters)
                        {
                            functionVariables.Set(p.Name, p.DefaultValue);
                        }

                        if (childNode.Attributes is not null)
                        {
                            foreach (XmlAttribute attr in childNode.Attributes)
                            {
                                if (attr.Name.Equals(SetContextVariableTag, StringComparison.OrdinalIgnoreCase))
                                {
                                    functionOutputs.Add(attr.InnerText);
                                }
                                else if (attr.Name.Equals(AppendToResultTag, StringComparison.OrdinalIgnoreCase))
                                {
                                    functionOutputs.Add(attr.InnerText);
                                    functionResults.Add(attr.InnerText);
                                }
                                else
                                {
                                    functionVariables.Set(attr.Name, attr.InnerText);
                                }
                            }
                        }

                        planStep.Outputs = functionOutputs;
                        planStep.Parameters = functionVariables;
                        foreach (var result in functionResults)
                        {
                            plan.Outputs.Add(result);
                        }

                        plan.AddSteps(planStep);
                    }
                }
            }
        }

        return plan;
    }

    private static void GetFunctionCallbackNames(string pluginFunctionName, out string pluginName, out string functionName)
    {
        var pluginFunctionNameParts = pluginFunctionName.Split('.');
        pluginName = pluginFunctionNameParts.Length > 1 ? pluginFunctionNameParts[0] : string.Empty;
        functionName = pluginFunctionNameParts?.Length > 1 ? pluginFunctionNameParts[1] : pluginFunctionName;
    }

    private static readonly string[] FunctionTagArray = new string[] { FunctionTag };
}
