using System.Text.Json;
using SemanticKernel.Function;
using SemanticKernel.Prompt;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public sealed class ModelResult
{
    private readonly object _result;

    public ModelResult(object result)
    {
        Verify.NotNull(result);

        _result = result;
    }

    public object GetRawResult() => _result;

    public T GetResult<T>()
    {
        if (_result is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {_result.GetType()} to {typeof(T)}");
    }

    public JsonElement GetJsonResult()
    {
        return Json.Deserialize<JsonElement>(_result.ToJson());
    }
}
