using AISmarteasy.Core.Connector.Web;

namespace Plugins.Native.Skills;

public sealed class GoogleSkill: WebSearchEngineSkill 
{
    private const string API_KEY = "";
    private const string SEARCH_ENGINE_ID = "";

    public GoogleSkill()
    {
        var connector = new GoogleConnector(
            apiKey: API_KEY,
            searchEngineId: SEARCH_ENGINE_ID);
        
        Connector = connector;
    }
}
