// See https://aka.ms/new-console-template for more information
using DogFriendlyParkFinder.Core;
using DogFriendlyParkFinder.Extractors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTransient<BrisbaneLoganCityParkExtractor>();
//builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
//host.Run();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Hello, World!");

var configuration = host.Services.GetRequiredService<IConfiguration>();

var googleMapApiKey = configuration.GetValue<string>("api:google_map:key") ?? string.Empty;
//var extractor = new BrisbaneLoganCityParkExtractor();

var extractor = host.Services.GetRequiredService<BrisbaneLoganCityParkExtractor>();
var dogFriendlyParks = extractor.Extract();

Console.WriteLine($"Dog friendly parks in Brisbane and Logan City:");

var dogFriendlyParkRecords = new List<ParkRecord>();

await foreach (var park in dogFriendlyParks)
{
    logger.LogInformation(park.ToString());
    dogFriendlyParkRecords.Add(park);
}

// load the content of the file static_html_template.html and replace the "{{REPLACE_COORDINATES}}" placeholder with the latitude longitude of the dogFriendlyParkRecords.
var staticHtmlTemplate = await File.ReadAllTextAsync("static_html_template.html");
var staticHtml = staticHtmlTemplate.Replace("{{REPLACE_COORDINATES}}", string.Join(",", dogFriendlyParkRecords.Select(p => $"{{ lat:{p.Latitude}, lng:{p.Longitude} }}")));
staticHtml = staticHtml.Replace("YOUR_API_KEY", googleMapApiKey);

// save the result to a new file called index.html
await File.WriteAllTextAsync("index.html", staticHtml);
