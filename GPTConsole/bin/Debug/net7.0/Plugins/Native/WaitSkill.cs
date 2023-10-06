using System.ComponentModel;
using AISmarteasy.Core.Function;

namespace Plugins.Native.Skills;

public interface IWaitProvider
{
    Task DelayAsync(int milliSeconds);
}

public sealed class WaitProvider : IWaitProvider
{
    public Task DelayAsync(int milliSeconds)
    {
        return Task.Delay(milliSeconds);
    }
}

public sealed class WaitSkill
{
    private readonly IWaitProvider _waitProvider;

    public WaitSkill()
    {
        _waitProvider = new WaitProvider();
    }

    [SKFunction, Description("Wait a given amount of seconds")]
    public async Task SecondsAsync([Description("The number of seconds to wait")] decimal seconds)
    {
        var milliseconds = seconds * 1000;
        milliseconds = milliseconds > 0 ? milliseconds : 0;

        await _waitProvider.DelayAsync((int)milliseconds).ConfigureAwait(false);
    }
}
