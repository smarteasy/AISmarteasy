using System.Text.RegularExpressions;
using System.Xml;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public static class SequentialPlanParser
{
    internal const string SOLUTION_TAG = "plan";
    internal const string FUNCTION_TAG = "function.";
    internal const string SET_CONTEXT_VARIABLE_TAG = "setContextVariable";
    internal const string APPEND_TO_RESULT_TAG = "appendToResult";

    public static void ToPlanFromXml(string xmlString, Plan plan, bool allowMissingFunctions = false)
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

        var solution = xmlDoc.GetElementsByTagName(SOLUTION_TAG);

        foreach (XmlNode solutionNode in solution)
        {
            foreach (XmlNode childNode in solutionNode.ChildNodes)
            {
                if (childNode.Name == "#text" || childNode.Name == "#comment")
                {
                    continue;
                }

                if (childNode.Name.StartsWith(FUNCTION_TAG, StringComparison.OrdinalIgnoreCase))
                {
                    var pluginFunctionName = childNode.Name.Split(FunctionTagArray, StringSplitOptions.None)[1];
                    GetFunctionCallbackNames(pluginFunctionName, out var stepPluginName, out var stepFunctionName);

                    if (!string.IsNullOrEmpty(stepFunctionName))
                    {
                        var function = (SemanticFunction)KernelProvider.Kernel!.FindFunction(stepPluginName, stepFunctionName);
                        var planStep = new Plan(function.PromptTemplate, function.PluginName, function.Name, function.Description);

                        var parameterViews = new Dictionary<string, ParameterView>();
                        var functionOutputs = new List<string>();
                        var functionResults = new List<string>();

                        foreach (var p in function.Parameters)
                        {
                            parameterViews.Add(p.Name, new ParameterView(p.Name, p.DefaultValue));
                        }

                        if (childNode.Attributes is not null)
                        {
                            foreach (XmlAttribute attr in childNode.Attributes)
                            {
                                if (attr.Name.Equals(SET_CONTEXT_VARIABLE_TAG, StringComparison.OrdinalIgnoreCase))
                                {
                                    functionOutputs.Add(attr.InnerText);
                                }
                                else if (attr.Name.Equals(APPEND_TO_RESULT_TAG, StringComparison.OrdinalIgnoreCase))
                                {
                                    functionOutputs.Add(attr.InnerText);
                                    functionResults.Add(attr.InnerText);
                                }
                                else
                                {
                                    parameterViews[attr.Name].DefaultValue = attr.InnerText;
                                }
                            }
                        }

                        planStep.Outputs = functionOutputs;
                        planStep.Parameters = parameterViews.Values.ToList();

                        foreach (var result in functionResults)
                        {
                            plan.Outputs.Add(result);
                        }

                        plan.AddSteps(planStep);
                    }
                }
            }
        }
    }

    private static void GetFunctionCallbackNames(string pluginFunctionName, out string pluginName, out string functionName)
    {
        var pluginFunctionNameParts = pluginFunctionName.Split('.');
        pluginName = pluginFunctionNameParts.Length > 1 ? pluginFunctionNameParts[0] : string.Empty;
        functionName = pluginFunctionNameParts.Length > 1 ? pluginFunctionNameParts[1] : pluginFunctionName;
    }

    private static readonly string[] FunctionTagArray = new string[] { FUNCTION_TAG };
}
