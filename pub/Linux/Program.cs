using NuGetFeed.Extensions;

namespace NuGetFeed.Linux;

public class Program
{
    public static void Main(string[] args)
    {
        //HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        //builder.Configuration
        //    .Sources.Clear();
        //builder.Configuration
        //    .AddIniFile("NuGetFeed.ini", true, true);


        // --port 3333 --proxy, everything else through NuGetFeed.ini
        // maybe "secret" --migrate/upgrade/initialize switch for updating sqlite schema file.

        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");

            if (!string.IsNullOrEmpty(root))
            {
                config.SetBasePath(root);
            }
        });
        builder.ConfigureWebHostDefaults(web =>
        {
            web.ConfigureKestrel(options =>
            {
                // Remove the upload limit from Kestrel. If needed, an upload limit can
                // be enforced by a reverse proxy server, like IIS.
                options.Limits.MaxRequestBodySize = null;
            });

            web.UseStartup<Startup>();
        });
        builder.UseSystemd();

        var host = builder.Build();
        if (!host.ValidateStartupOptions())
        {
            return;
        }

        host.RunMigrationsAsync().Wait();
        host.RunAsync().Wait();
    }

}
