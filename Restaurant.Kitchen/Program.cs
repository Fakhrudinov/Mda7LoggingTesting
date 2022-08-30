using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Restaurant.Kitchen
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => 
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseUrls(urls: "http://localhost:5000");
                    })
                .ConfigureLogging(log =>
                    {
                        log.SetMinimumLevel(LogLevel.Warning);
                    });
        }
    }
}