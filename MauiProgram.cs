using Code.OutfitPatcher;
using Code.OutfitPatcher.Config;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using OutfitPatcher.Data;
using Radzen;
using System.Reflection;

namespace OutfitPatcher;

public static class MauiProgram
{	
	public static MauiApp CreateMauiApp()
	{
        // Init logger
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new System.IO.FileInfo("log4net.config"));

	    var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();
		builder.Services.AddSingleton<SynPatch>();


        return builder.Build();
	}
}
