namespace AISmarteasy.Core;

public static class Telemetry
{
    private const string TELEMETRY_DISABLED_ENV_VAR = "AZURE_TELEMETRY_DISABLED";

    public const string HTTP_USER_AGENT = "Semantic-Kernel";

    public static bool IsTelemetryEnabled => !EnvExtensions.GetBoolEnvVar(TELEMETRY_DISABLED_ENV_VAR) ?? true;
}