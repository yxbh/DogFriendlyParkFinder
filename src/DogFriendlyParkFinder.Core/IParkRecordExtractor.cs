namespace DogFriendlyParkFinder.Core;

/// <summary>
/// Interface for implementing extractors that can extract dog friendly park information from a source.
/// </summary>
public interface IParkRecordExtractor
{
    /// <summary>
    /// Extract all available park records and return them.
    /// </summary>
    /// <returns></returns>
    public IAsyncEnumerable<ParkRecord> Extract();
}
