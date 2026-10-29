# Configuration

You can modify BaGet's configurations by editing the `appsettings.json` file.

## Require an API key

The feed is open until you add API keys: anyone can read, push and delete packages. The Windows
and Linux hosts check keys, with their roles, as soon as any are configured. Each key has a role:

| Role | May |
| --- | --- |
| `ReadOnly` | Read the feed: search, metadata, download packages and symbols, the catalog and vulnerabilities. |
| `ReadWrite` | Everything `ReadOnly` may, plus push packages and symbols, and unlist (soft delete) and relist packages. |
| `Admin` | Everything, including permanently deleting packages, replacing packages, deprecating packages and managing vulnerabilities. |
| `None` | Nothing. Only useful as the `AnonymousRole`. |

```json
{
    "Authorization": {
        // What requests without a valid key may do. If unset, ReadOnly.
        "AnonymousRole": "ReadOnly",
        "ApiKeys": [
            { "Name": "Administrator", "Key": "ADMIN-SECRET", "Role": "Admin" },
            { "Name": "Build server", "Key": "BUILD-SECRET", "Role": "ReadWrite" },
            { "Name": "Developers", "Key": "READ-SECRET", "Role": "ReadOnly" }
        ]
    },
    ...
}
```

The `ApiKey` setting still works, and is an `Admin` key. A key never takes away what anonymous
requests may do. In `IsReadOnlyMode`, nobody may change the feed, whatever their key.

Which permission deleting a package needs depends on `PackageDeletionBehavior`: `Unlist` (the
default) is a soft delete that `ReadWrite` keys may do, and `HardDelete` needs an `Admin` key.
With `AllowPackageOverwrites` on, only `Admin` keys replace an existing package; others get a 409.

Requests without enough permission get a 401 if they have no valid key, and a 403 if their key's
role isn't enough.

### Applications that host the feed

An application that hosts the server library chooses whether it checks keys, by what it adds:

| The application adds | The feed |
| --- | --- |
| Neither | Is open. |
| `services.AddAuthentication().AddNuGetFeedApiKey()` | Requires a valid API key to do what anonymous requests can't. Any key may do everything. |
| That, and `services.AddAuthorizationBuilder().AddNuGetFeedPolicies()` | Gives each API key its role. |

The application calls `app.UseAuthentication()` and `app.UseAuthorization()` after `app.UseRouting()`.
The application doesn't start if the settings don't match what it adds: keys or an `AnonymousRole`
it would ignore, authentication without keys, roles without the policies, or policies without roles.

### Using a key

Clients present a key either in the `X-NuGet-ApiKey` header, which is what the push command's
`-k` option sends:

```sh
dotnet nuget push -s https://nuget.example.com/v3/index.json -k BUILD-SECRET package.1.0.0.nupkg
```

or as the password of the package source's credentials in a `nuget.config` file. NuGet sends
those whenever the feed asks for them, for restore, browsing in Visual Studio and push, so this is
the way to give Visual Studio a key. The user name isn't used, but NuGet requires one:

```xml
<configuration>
  <packageSources>
    <add key="NuGetFeed" value="https://nuget.example.com/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <NuGetFeed>
      <add key="Username" value="me" />
      <add key="ClearTextPassword" value="BUILD-SECRET" />
    </NuGetFeed>
  </packageSourceCredentials>
</configuration>
```

Visual Studio reads `%AppData%\NuGet\NuGet.Config` and any `nuget.config` next to your solution.
You can also add the credentials with
`dotnet nuget update source NuGetFeed --username me --password BUILD-SECRET --store-password-in-clear-text`.
Without `--store-password-in-clear-text`, NuGet saves an encrypted `Password` instead of
`ClearTextPassword` on Windows. The encrypted password only works for your user on that machine.

When pushing with only these credentials, NuGet warns "No API Key was provided". That's
harmless: the feed asks for credentials and NuGet retries the push with them.

To push without that warning, save the key for the source with `setapikey`. NuGet then sends it
in the `X-NuGet-ApiKey` header on every push and delete:

```sh
dotnet nuget setapikey BUILD-SECRET -s https://nuget.example.com/v3/index.json
```

The key goes in the `apikeys` section of your user `NuGet.Config`, encrypted on Windows, so it
can't be written by hand or copied to another machine: run `setapikey` on each machine that pushes.
NuGet doesn't send a saved API key when restoring or browsing, so it doesn't help Visual Studio
read a feed that anonymous users can't read.

Which to use:

| You want to | Use |
| --- | --- |
| Restore and browse in Visual Studio, when anonymous users can't read the feed | `packageSourceCredentials` |
| Push from a machine, every time | `setapikey`, or `packageSourceCredentials` |
| Push once, or from a build server's secret | `dotnet nuget push -k` |

Use HTTPS, since the key travels with every request. For an `http://` source, NuGet needs
`allowInsecureConnections="true"` on the package source.

If the website is hosted with a feed that anonymous users can't read, give it a `ReadOnly` key in
`Website:ApiKey`. Everyone who can open the website sees what that key can read.

## Enable read-through caching

Read-through caching lets you index packages from an upstream source. You can use read-through
caching to:

1. Speed up your builds if restores from [nuget.org](https://nuget.org) are slow
1. Enable package restores in offline scenarios

The following `Mirror` setting configures BaGet to index packages from [nuget.org](https://nuget.org):

```json
{
    ...

    "Mirror": {
        "Enabled":  true,
        "PackageSource": "https://api.nuget.org/v3/index.json"
    },

    ...
}
```

!!! info
    `PackageSource` is the value of the [NuGet service index](https://docs.microsoft.com/en-us/nuget/api/service-index).

## Enable package hard deletions

To prevent the ["left pad" problem](https://blog.npmjs.org/post/141577284765/kik-left-pad-and-npm),
BaGet's default configuration doesn't allow package deletions. Whenever BaGet receives a package deletion
request, it will instead "unlist" the package. An unlisted package is undiscoverable but can still be
downloaded if you know the package's id and version. You can override this behavior by setting the
`PackageDeletionBehavior`:

```json
{
    ...

    "PackageDeletionBehavior": "HardDelete",

    ...
}
```

## Enable package overwrites

Normally, BaGet will reject a package upload if the id and version are already taken. You can configure BaGet
to overwrite the already existing package by setting `AllowPackageOverwrites`:

```json
{
    ...

    "AllowPackageOverwrites": true,

    ...
}
```

## Private feeds

A private feed requires users to authenticate before accessing packages.

!!! warning
    Private feeds are not supported at this time! See [this pull request](https://github.com/loic-sharma/BaGet/pull/69) for more information.

## Database configuration

BaGet supports multiple database engines for storing package information:


- MySQL: `MySql`
- SQLite: `Sqlite`
- SQL Server: `SqlServer`
- PostgreSQL: `PostgreSql`
- Azure Table Storage: `AzureTable`

Each database engine requires a connection string to configure the connection. Please refer to [ConnectionStrings.com](https://www.connectionstrings.com/) to learn how to create the proper connection string for each database engine.

You may configure the chosen database engine either using environment variables or by editing the `appsettings.json` file.

### Environment Variables

There are two environment variables related to database configuration. These are:

- **Database__Type**: The database engine to use, this should be one of the strings from the above list such as `PostgreSql` or `Sqlite`.
- **Database__ConnectionString**: The connection string for your database engine.

### `appsettings.json`

The database settings are located under the `Database` key in the `appsettings.json` configuration file:

```json
{
    ...

    "Database": {
        "Type": "Sqlite",
        "ConnectionString": "Data Source=baget.db"
    },

    ...
}
```

There are two settings related to the database configuration:

- **Type**: The database engine to use, this should be one of the strings from the above list such as `PostgreSql` or `Sqlite`.
- **ConnectionString**: The connection string for your database engine.

## IIS server options

IIS Server options can be configured under the `IISServerOptions` key. The available options are detailed at [docs.microsoft.com](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.iisserveroptions)
Note: If not specified, the MaxRequestBodySize in BaGet defaults to 250MB (262144000 bytes), rather than the ASP.NET Core default of 30MB

```json
{
    ...

    "IISServerOptions": {
        "MaxRequestBodySize": 262144000
    },

    ...
}
```
