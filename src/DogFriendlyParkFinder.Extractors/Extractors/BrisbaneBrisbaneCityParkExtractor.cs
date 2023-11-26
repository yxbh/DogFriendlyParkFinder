using DogFriendlyParkFinder.Core;
using GoogleMaps.LocationServices;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DogFriendlyParkFinder.Extractors;

/// <summary>
/// Dog friend park extraction for Brisbane Brisbane.
/// </summary>
public class BrisbaneBrisbaneCityParkExtractor : IParkRecordExtractor
{
    private string _apiKey;

    private readonly ILogger<BrisbaneLoganCityParkExtractor> _logger;

    public BrisbaneBrisbaneCityParkExtractor(IConfiguration configuration, ILogger<BrisbaneLoganCityParkExtractor> logger)
    {
        _apiKey = configuration.GetValue<string>("api:google_map:key") ?? string.Empty;
        _logger = logger;
    }

    public async IAsyncEnumerable<ParkRecord> Extract()
    {
        var httpClient = new HttpClient();
        var url = "https://www.brisbane.qld.gov.au/things-to-see-and-do/council-venues-and-precincts/parks/park-facilities/dog-off-leash-areas-dog-parks";
        _logger.LogDebug($"Loading {url}");
        var html = await httpClient.GetStringAsync(url);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        // extract div with class="map-studio-map__tables" which contains a table of parks.
        var tableDivNode = htmlDocument.DocumentNode.Descendants("div")
            .Where(node => node.GetAttributeValue("class", "")
                       .Contains("map-studio-map__tables")).FirstOrDefault();
        var tableNode = tableDivNode!.Descendants("table").First();
        var tableBodyNode = tableNode.Descendants("tbody").First();

        var gls = new GoogleLocationService(_apiKey);

        // iterate all rows in tableBodyNode and extract 3 columns: park name, locationand description (contains park URL).
        foreach (var tableRowNode in tableBodyNode.Descendants("tr"))
        {
            var tableDataNodes = tableRowNode.Descendants("td").ToList();
            var parkName = tableDataNodes[0].InnerText.Trim();
            var location = tableDataNodes[1].InnerText.Trim();
            var description = tableDataNodes[2].InnerText.Trim();
            var parkUrl = tableDataNodes[2].Descendants("a").First().GetAttributeValue("href", "");

            //
            // NOTE: The stupid Brisbane City Council website's park URLs require Javascript to load the park data.
            // Since we don't really have a way to easily run Javascript in .NET we use Google Geocoding Service to grab the coordinate from the park address.
            //

            var latlong = gls.GetLatLongFromAddress(location); // returns null if address not found.


            //var parkHtmlDoc = new HtmlDocument();
            //_logger.LogDebug($"Loading {parkUrl}");
            //var parkHtml = await httpClient.GetStringAsync(parkUrl);
            //parkHtmlDoc.LoadHtml(parkHtml);

            //var parkLocationNode = parkHtmlDoc.DocumentNode.Descendants("th")
            //    .Where(node => node.GetAttributeValue("class", "")
            //                   .Equals("twEDLabel") && node.InnerText.Trim().Contains("Location")).First();

            //var paprkLocationAnchorNode = parkLocationNode!.ParentNode.Descendants("a").First();
            //var parkLocationAnchorHref = paprkLocationAnchorNode?.GetAttributeValue("href", "");
            //var parkLocationAnchorHrefParts = parkLocationAnchorHref?.Split("&query=").Last().Split("(").First().Split(",");

            yield return new ParkRecord
            (
                parkName,
                location,
                url
            )
            {
                Url = parkUrl,
                Latitude = latlong?.Latitude ?? 0.0,
                Longitude = latlong?.Longitude ?? 0.0,
            };
        }
    }
}
