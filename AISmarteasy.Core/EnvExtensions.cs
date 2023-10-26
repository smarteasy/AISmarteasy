namespace AISmarteasy.Core;

internal static class EnvExtensions
{
    internal static bool? GetBoolEnvVar(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (string.Equals(bool.TrueString, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals("1", value, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(bool.FalseString, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals("0", value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }
}
