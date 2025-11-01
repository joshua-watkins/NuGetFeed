# NuGet Feed

[NuGet] and [symbol] server.

## Getting Started

1. Install the [.NET SDK]
2. Download and extract the [latest release]
3. Start the service with `dotnet NuGetFeed.dll`
4. Browse `http://localhost:5000/` in your browser

## Features

* This project serves to replace the abandoned NuGet.Server project by Microsoft.
* Runs on Windows, Linux, and macOS.
* Updated to latest .NET LTS release.

TBD if it's worth maintaining mirroring.
Offline support: [mirror a NuGet feed] to speed up builds and enable offline downloads

[NuGet]: https://learn.microsoft.com/nuget/what-is-nuget
[symbol]: https://docs.microsoft.com/en-us/windows/desktop/debug/symbol-servers-and-symbol-stores
[.NET SDK]: https://www.microsoft.com/net/download
[latest release]: https://github.com/joshua-watkins/NuGetFeed/releases
[mirror a NuGet feed]: https://loic-sharma.github.io/BaGet/configuration/#enable-read-through-caching
