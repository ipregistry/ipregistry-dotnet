// A small tour of the Ipregistry .NET client from F#. Run with:
//
//   IPREGISTRY_API_KEY=YOUR_API_KEY dotnet run --project samples/fsharp

open System
open Ipregistry

[<EntryPoint>]
let main _ =
    match Environment.GetEnvironmentVariable "IPREGISTRY_API_KEY" with
    | null
    | "" ->
        eprintfn "Set the IPREGISTRY_API_KEY environment variable to run this sample."
        1
    | apiKey ->
        use client =
            new IpregistryClient(IpregistryClientOptions(ApiKey = apiKey, Cache = InMemoryIpregistryCache()))

        task {
            // Single IP lookup.
            let! info = client.LookupAsync "66.165.2.7"
            printfn $"{info.Ip}: {info.Location.City}, {info.Location.Country.Name}"

            // Origin lookup: the IP address this program appears to come from.
            let! origin = client.LookupOriginAsync()
            printfn $"You are {origin.Ip} in {origin.Location.Country.Name}"

            // Batch lookup: entries may independently succeed or fail.
            let! results = client.LookupBatchAsync [ "8.8.8.8"; "1.1.1.1"; "not-an-ip" ]

            for result in results do
                match result.Error with
                | null -> printfn $"{result.Value.Ip} -> {result.Value.Location.Country.Name}"
                | error -> printfn $"failed -> {error.Message}"

            // User-Agent parsing.
            let! agents = client.ParseUserAgentsAsync [ "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Safari/605.1.15" ]
            printfn $"Parsed agent: {agents[0].Value.Name}"
        }
        |> fun t -> t.GetAwaiter().GetResult()

        0
