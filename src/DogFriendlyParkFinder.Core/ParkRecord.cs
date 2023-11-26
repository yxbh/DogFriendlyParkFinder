namespace DogFriendlyParkFinder.Core;

public record ParkRecord(string Name, string Location, string Source)
{
    public string? Url { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public override string ToString()
    {
        return $"Name: \"{Name}\", Location: \"{Location}\", Url: \"{Url}\"";
    }
}
