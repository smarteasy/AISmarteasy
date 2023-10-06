using AISmarteasy.Core.Connector.Web;

namespace Plugins.Native.Skills;

public sealed class GoogleSkill: WebSearchEngineSkill 
{
    private const string API_KEY = "";
    private const string SEARCH_ENGINE_ID = "0656a4c79b69f47c5";

    public GoogleSkill()
    {
        var connector = new GoogleConnector(
            apiKey: API_KEY,
            searchEngineId: SEARCH_ENGINE_ID);
        
        Connector = connector;
    }
}
