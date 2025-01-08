using InventoryManagement.WebAPI;

var builder = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

builder.Build().Run();

public partial class Program { }
