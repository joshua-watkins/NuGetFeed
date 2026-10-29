# NuGetFeed.Website

A nuget.org-style website for browsing a NuGet V3 feed. It only reads the feed through the
NuGet V3 protocol, so it can show this solution's server or any other V3 feed.

The pages are [Razor Slices](https://github.com/DamianEdwards/RazorSlices) in the `Slices` folder,
served by minimal API endpoints. Each page's data comes from a model in the `Pages` folder.

## Running it on its own

Set `Website:ServiceIndexUrl` to the feed's service index. In Development, it defaults to the
server project's launch URL, so starting both projects is enough:

```sh
dotnet run --project src/Server     # The feed, at http://localhost:3333/v3/index.json
dotnet run --project src/Website    # The website, at http://localhost:3334/
```

To show another feed:

```sh
dotnet run --project src/Website -- --Website:ServiceIndexUrl=https://api.nuget.org/v3/index.json
```

If the website can't read the feed, its pages explain that instead of failing.

## Hosting it with the server

The Windows and Linux hosts in `pub` add the website to the server's application with
`services.AddNuGetFeedUI()` and `endpoints.MapNuGetFeedUI()`. Leave `Website:ServiceIndexUrl`
empty there: the website then reads the feed from its own application.

## Native AOT

The website can be published with native AOT on its own:

```sh
dotnet publish src/Website -c Release -r win-x64 -p:PublishAot=true
```
