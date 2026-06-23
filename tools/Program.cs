using RemTool.Shared;

using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace RemTools
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ExcelPackage.License.SetNonCommercialOrganization("DESAM Monte Patria");

            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddDbContext<RemToolDataContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
            });

            builder.Services.AddTransient<Main>();
            builder.Services.AddTransient<Utils>();

            var app = builder.Build();

            ApplicationConfiguration.Initialize();

            Application.Run(app.Services.GetRequiredService<Main>());
           


            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            //Application.Run(new Main());

        }
    }
}