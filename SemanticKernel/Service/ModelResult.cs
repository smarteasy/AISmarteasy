using System.Text.Json;
using SemanticKernel.Function;
using SemanticKernel.Prompt;

#pragma warning disable CA1024

namespace SemanticKernel.Service;

public sealed class ModelResult
{
    private readonly object result;

    public ModelResult(object result)
    {
        Verify.NotNull(result);

        this.result = result;
    }

    public object GetRawResult() => result;

    public T GetResult<T>()
    {
        if (result is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {result.GetType()} to {typeof(T)}");
    }

    public JsonElement GetJsonResult()
    {
        return Json.Deserialize<JsonElement>(result.ToJson());
    }
}
