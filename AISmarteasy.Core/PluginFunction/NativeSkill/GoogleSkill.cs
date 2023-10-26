using AISmarteasy.Core.Connecting.Google;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class GoogleSkill: WebSearchEngineSkill 
{
    public GoogleSkill()
    {
        var connector = new GoogleConnector(
            apiKey: Env.GoogleAPIKey,
            searchEngineId: Env.SearchEngineId);
        
        Connector = connector;
    }
}
