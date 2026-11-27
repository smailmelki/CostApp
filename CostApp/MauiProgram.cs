using CommunityToolkit.Maui;
using CostApp.Models;
using Microsoft.Extensions.Logging;

namespace CostApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            DBContext.EnsureDatabaseReady();
            CostDataService.GetOrEnsureActiveProject();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
