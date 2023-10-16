using AISmarteasy.Core.Connector.Web;
using GPTConsole;

namespace Plugins.Native.Skills;

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
