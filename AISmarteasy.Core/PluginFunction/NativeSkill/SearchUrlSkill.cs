
using System.ComponentModel;
using System.Text.Encodings.Web;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class SearchUrlSkill
{
    [SKFunction, Description("Return URL for Amazon search query")]
    public string AmazonSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.amazon.com/s?k={encoded}";
    }


    [SKFunction, Description("Return URL for Bing search query.")]
    public string BingSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for Bing Images search query.")]
    public string BingImagesSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/images/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for Bing Maps search query.")]
    public string BingMapsSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/maps?q={encoded}";
    }

    [SKFunction, Description("Return URL for Bing Shopping search query.")]
    public string BingShoppingSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/shop?q={encoded}";
    }

    [SKFunction, Description("Return URL for Bing News search query.")]
    public string BingNewsSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/news/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for Bing Travel search query.")]
    public string BingTravelSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.bing.com/travel/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for Facebook search query.")]
    public string FacebookSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.facebook.com/search/top/?q={encoded}";
    }

    [SKFunction, Description("Return URL for GitHub search query.")]
    public string GitHubSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://github.com/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for LinkedIn search query.")]
    public string LinkedInSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://www.linkedin.com/search/results/index/?keywords={encoded}";
    }

    [SKFunction, Description("Return URL for Twitter search query.")]
    public string TwitterSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://twitter.com/search?q={encoded}";
    }

    [SKFunction, Description("Return URL for Wikipedia search query.")]
    public string WikipediaSearchUrl([Description("Text to search for")] string query)
    {
        string encoded = UrlEncoder.Default.Encode(query);
        return $"https://wikipedia.org/w/index.php?search={encoded}";
    }
}
