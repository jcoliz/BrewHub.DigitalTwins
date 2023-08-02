using System.Reflection;
using BrewHub.DigitalTwins.Replicator;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context,services) =>
    {
        // https://stackoverflow.com/questions/41287648/how-do-i-write-logs-from-within-startup-cs
        // https://github.com/dotnet/aspnetcore/issues/9337#issuecomment-539859667
        using var loggerFactory = LoggerFactory.Create(logbuilder =>
        {
            logbuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
            logbuilder.AddConsole();
            logbuilder.AddEventSourceLogger();
        });
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("*** STARTING ***");

        // Get app version, store in configuration for later use
        var assembly = Assembly.GetEntryAssembly();
        var resource = assembly!.GetManifestResourceNames().Where(x => x.EndsWith(".version.txt")).SingleOrDefault();
        if (resource is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            using var streamreader = new StreamReader(stream!);
            var version = streamreader.ReadLine();
            context.Configuration["Codebase:Version"] = version;
            logger.LogInformation("Version: {version}", version);
        }
        
        services.AddHostedService<Worker>();
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();
