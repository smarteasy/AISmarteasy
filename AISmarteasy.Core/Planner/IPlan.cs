using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Planner;

public interface IPlan : ISKFunction
{
    Task RunAsync(AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
    string Content { get; set; }
    IList<string> Outputs { get; }
    void AddSteps(Plan value);
    bool HasNextStep { get; }
}
