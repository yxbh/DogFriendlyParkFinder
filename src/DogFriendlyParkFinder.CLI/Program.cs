// See https://aka.ms/new-console-template for more information
using DogFriendlyParkFinder.Extractors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Hello, World!");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddTransient<BrisbaneLoganCityParkExtractor>();
//builder.Services.AddHostedService<Worker>();

//builder.Configuration.AddJsonFile("appsettings.json", optional: false);

IHost host = builder.Build();
//host.Run();


var configuration = host.Services.GetRequiredService<IConfiguration>();
var extractor = new BrisbaneLoganCityParkExtractor(configuration.GetValue<string>("api:google_map:key") ?? string.Empty);

//var extractor = host.Services.GetRequiredService<BrisbaneLoganCityParkExtractor>();

var dogFriendlyParks = extractor.Extract();

Console.WriteLine($"Dog friendly parks in Brisbane and Logan City:");
await foreach (var park in dogFriendlyParks)
{
    Console.WriteLine(park);
}