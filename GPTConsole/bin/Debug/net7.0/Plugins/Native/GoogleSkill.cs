using AISmarteasy.Core.Connector.Web;
using GPTConsole;

namespace Plugins.Native.Skills;

public sealed class GoogleSkill: WebSearchEngineSkill 
{
    public GoogleSkill()
    {
        var connector = new GoogleConnector(
            apiKey: Env.GOOGLE_API_KEY,
            searchEngineId: Env.SEARCH_ENGINE_ID);
        
        Connector = connector;
    }
}
