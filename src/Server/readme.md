# NuGetFeed.Server

The backend that implements the [NuGet service APIs](https://docs.microsoft.com/en-us/nuget/api/overview)
and persists packages. Run it directly as the feed's HTTP server (`dotnet run`, then `http://localhost:3333/v3/index.json`),
or add it to another host with `services.AddNuGetFeed(...)`, `app.UseNuGetFeed()` and `endpoints.MapNuGetFeed()`.
The important folders are:

* Controllers - The NuGet V3 API endpoints
* Indexing - Logic to upload and delete packages and symbols
* Metadata, Content, Search, ServiceIndex - Server implementations of the protocol resources
* Entities - The package database (SQLite by default)
* Storage - Stores the content of packages and symbols on the file system
* Upstream - Interactions with an upstream NuGet feed when mirroring
