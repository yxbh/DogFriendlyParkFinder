using DogFriendlyParkFinder.Core;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogFriendlyParkFinder.Extractors;

public class BrisbaneLoganCityParkExtractor : IParkRecordExtractor
{
    private string _apiKey;

    public BrisbaneLoganCityParkExtractor(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async IAsyncEnumerable<ParkRecord> Extract()
    {
        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync("https://www.logan.qld.gov.au/directory/search?directoryID=1&showInMap=&keywords=off+leash&categoryId=&postcode=&search=Search");

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        // extract list items from https://www.logan.qld.gov.au/directory/search?directoryID=1&showInMap=&keywords=off+leash&categoryId=&postcode=&search=Search where the ul element contains a class of "list--record_parks_results"
        // for each list item, extract the <a> element with class="list__link", get the node text and href attribute value. This is the park name and link to the park details page

        var parkList = htmlDocument.DocumentNode.Descendants("ul")
            .Where(node => node.GetAttributeValue("class", "")
            .Equals("list list--record_parks_results")).FirstOrDefault();

        var parkListItems = parkList.Descendants("li")
            .Where(node => node.GetAttributeValue("class", "")
                       .Contains("list__item")).ToList();

        foreach (var parkListItem in parkListItems)
        {
            var parkNode = parkListItem.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "")
                                          .Equals("list__link")).FirstOrDefault();
            var parkName = parkNode?.InnerText;
            var parkUrl = parkNode?.GetAttributeValue("href", "");

            var parkRecord = new ParkRecord
            (
                parkName ?? string.Empty,
                "Logan City, Brisbane, QLD, AU",
                "https://www.logan.qld.gov.au/directory/search?directoryID=1&showInMap=&keywords=off+leash&categoryId=&postcode=&search=Search"
            )
            {
                Url = $"https://www.logan.qld.gov.au{parkUrl}",
            };

            yield return parkRecord;
        }
    }
}
