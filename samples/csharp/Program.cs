// A small tour of the Ipregistry .NET client. Run with:
//
//   IPREGISTRY_API_KEY=YOUR_API_KEY dotnet run --project samples/csharp

using Ipregistry;

var apiKey = Environment.GetEnvironmentVariable("IPREGISTRY_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("Set the IPREGISTRY_API_KEY environment variable to run this sample.");
    return 1;
}

using var client = new IpregistryClient(new IpregistryClientOptions
{
    ApiKey = apiKey,
    Cache = new InMemoryIpregistryCache(),
});

// Single IP lookup.
var info = await client.LookupAsync("66.165.2.7");
Console.WriteLine($"{info.Ip}: {info.Location.City}, {info.Location.Country.Name} " +
                  $"(AS{info.Connection.Asn} {info.Connection.Organization})");

// Origin lookup: the IP address this program appears to come from.
var origin = await client.LookupOriginAsync();
Console.WriteLine($"You are {origin.Ip} in {origin.Location.Country.Name}");

// Batch lookup: entries may independently succeed or fail.
var results = await client.LookupBatchAsync("8.8.8.8", "1.1.1.1", "not-an-ip");
foreach (var result in results)
{
    Console.WriteLine(result.IsSuccess
        ? $"{result.Value.Ip} -> {result.Value.Location.Country.Name}"
        : $"failed -> {result.Error.Message}");
}

// User-Agent parsing.
var agents = await client.ParseUserAgentsAsync(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
Console.WriteLine($"Parsed agent: {agents[0].Value.Name} on {agents[0].Value.OperatingSystem.Name}");

return 0;
