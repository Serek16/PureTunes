using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace EmyProject;

public abstract class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
            });
}