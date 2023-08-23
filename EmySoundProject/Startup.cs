using System;
using System.Net.Http;
using EmySoundProject.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Radzen;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Emy;

namespace EmySoundProject;

public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped(serviceProvider =>
        {
            var uriHelper = serviceProvider.GetRequiredService<NavigationManager>();

            return new HttpClient
            {
                BaseAddress = new Uri(uriHelper.BaseUri)
            };
        });

        services.AddHttpClient();

        services.AddRazorPages();
        services.AddServerSideBlazor().AddHubOptions(o => { o.MaximumReceiveMessageSize = 10 * 1024 * 1024; });

        services.AddSingleton(Configuration);

        services.AddScoped<NotificationService>();
        services.AddSingleton<IFingerprintStorage, EmyDockerModelServiceAdapter>();
        services.AddSingleton<IAudioService, FFmpegAudioService>();
        services.AddScoped<AFMService>();
        services.AddScoped<AudioExtractionService>();

        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        var ffmpegExecutablePath = Configuration["FFmpegSettings:ExecutablePath"];
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(ffmpegExecutablePath);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger,
        IHostApplicationLifetime lifetime)
    {
        logger.LogInformation("Application is starting up.");

        if (env.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.Use((ctx, next) => next());
        }

        app.UseStaticFiles();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });

        lifetime.ApplicationStopping.Register(() => OnStopping(logger));
    }

    private void OnStopping(ILogger<Startup> logger)
    {
        logger.LogInformation("Application is stopping.");
    }
}