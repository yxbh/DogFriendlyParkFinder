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

            //
            // NOTE 2: The location column contains both the park name and the address.
            // This can cause issues with the Google Geocoding Service as it may not be able to find the address
            // and end up returning a partial match (i.e. coordinate to the suburb name instead of the park itself).
            // We should consider removing the park name of the location string, or just use the park name and hope Gecoding can figure the rest of the detail.
            //

            var latlong = gls.GetLatLongFromAddress(location); // returns null if address not found.

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
