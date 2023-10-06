using SemanticKernel.Util;

namespace SemanticKernel;

public static class Telemetry
{
    private const string TelemetryDisabledEnvVar = "AZURE_TELEMETRY_DISABLED";

    public const string HttpUserAgent = "Semantic-Kernel";

    public static bool IsTelemetryEnabled => !EnvExtensions.GetBoolEnvVar(TelemetryDisabledEnvVar) ?? true;
}