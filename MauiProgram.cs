using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CommunityToolkit.Maui;
using tipitipapp.Extention;

namespace tipitipapp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Console.WriteLine($"Environment: {environment}");

            if (string.IsNullOrEmpty(environment))
            {
                Console.WriteLine("INFO: Environment variable not setted (ASPNETCORE_ENVIRONMENT)");
                Console.WriteLine("Default Environment: Development");
                environment = "Development";
            }
            builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddDepedencyExtention(builder.Configuration);


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
