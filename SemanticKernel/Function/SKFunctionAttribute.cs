namespace SemanticKernel.Function;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SKFunctionAttribute : Attribute
{
    public SKFunctionAttribute()
    {
    }
}
