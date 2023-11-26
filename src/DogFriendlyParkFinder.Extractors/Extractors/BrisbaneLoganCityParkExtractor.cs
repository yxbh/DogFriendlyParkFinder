using DogFriendlyParkFinder.Core;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DogFriendlyParkFinder.Extractors;

public class BrisbaneLoganCityParkExtractor : IParkRecordExtractor
{
    private string _apiKey;

    private readonly ILogger<BrisbaneLoganCityParkExtractor> _logger;

    public BrisbaneLoganCityParkExtractor(ILogger<BrisbaneLoganCityParkExtractor> logger)
    {
        _apiKey = string.Empty;
        _logger = logger;
    }

    public async IAsyncEnumerable<ParkRecord> Extract()
    {
        var httpClient = new HttpClient();
        var url = "https://www.logan.qld.gov.au/directory/search?directoryID=1&showInMap=&keywords=off+leash&categoryId=&postcode=&search=Search";
        _logger.LogDebug($"Loading {url}");
        var html = await httpClient.GetStringAsync(url);

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
            parkUrl = $"https://www.logan.qld.gov.au{parkUrl}";

            var parkHtmlDoc = new HtmlDocument();
            _logger.LogDebug($"Loading {parkUrl}");
            var parkHtml = await httpClient.GetStringAsync(parkUrl);
            parkHtmlDoc.LoadHtml(parkHtml);

            // Park location HTML looks like this: <dt class="definition__heading" role="term listitem">Location</dt><dd class="definition__content definition__content--text-area" role="definition listitem"><a href = "https://www.google.com/maps/search/?api=1&amp;query=-27.66961106,153.0484354" class="external">Bennett Drive, Regents Park</a></dd>
            // Extract the <dd> element with class="definition__content definition__content--text - area" and get the <a> element with class="external" and get the href attribute value. This is the park location.

            var parkLocationNode = parkHtmlDoc.DocumentNode.Descendants("dt")
                .Where(node => node.GetAttributeValue("class", "")
                               .Equals("definition__heading") && node.InnerText.Trim().Equals("Location")).FirstOrDefault();

            // Do this by looking for a <a> inside a <dd> that has a href starting with "https://www.google.com/maps/search/".

            parkLocationNode = parkLocationNode!.NextSibling.NextSibling;

            //var parkLocationNode = parkHtmlDoc.DocumentNode.Descendants("a")
            //.Where(node => node.ParentNode.Name.Equals("dd") && node.GetAttributeValue("href", "")
            //               .StartsWith("https://www.google.com/maps/search/")).FirstOrDefault();
            var parkLocation = parkLocationNode?.InnerText.Trim();

            var paprkLocationAnchorNode = parkLocationNode!.Descendants("a").FirstOrDefault();
            var parkLocationAnchorHref = paprkLocationAnchorNode?.GetAttributeValue("href", "");
            var parkLocationAnchorHrefParts = parkLocationAnchorHref?.Split("&query=").Last().Split(",");

            var parkRecord = new ParkRecord
            (
                parkName ?? string.Empty,
                $"{parkLocation}, Logan City, Brisbane, QLD, AU",
                "https://www.logan.qld.gov.au/directory/search?directoryID=1&showInMap=&keywords=off+leash&categoryId=&postcode=&search=Search"
            )
            {
                Url = parkUrl,
                Latitude = double.Parse(parkLocationAnchorHrefParts![0]),
                Longitude = double.Parse(parkLocationAnchorHrefParts![1]),
            };

            yield return parkRecord;
        }
    }
}
