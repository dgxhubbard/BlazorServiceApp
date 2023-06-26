using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;


using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using NLog.Web;



namespace BlazorServiceApp.Server
{

    //
    // Refer to launchSettings.json
    // https://stackoverflow.com/questions/51738893/removing-kestrel-binding-warning
    //
    public class Program
    {
        public static void Main ( string [] args )
        {
            var path = Path.GetDirectoryName ( Assembly.GetExecutingAssembly ().Location );
            Directory.SetCurrentDirectory ( path );

            var logger = LogManager.Setup ().LoadConfigurationFromFile ( "nlog.config" ).GetCurrentClassLogger ();


            if ( !Ports.Exists ( path ) )
                Ports.SavePorts ( path );
            var ports = Ports.LoadPorts ( path );


            var config = new ConfigurationBuilder ()
                        .SetBasePath ( Directory.GetCurrentDirectory () )
                        .AddEnvironmentVariables ()
                        .AddJsonFile ( "certificate.json", optional: true, reloadOnChange: true )
                        .AddJsonFile ( $"certificate.{Environment.GetEnvironmentVariable ( "ASPNETCORE_ENVIRONMENT" )}.json", optional: true, reloadOnChange: true )
                        .Build ();

            var certificateSettings = config.GetSection ( "certificateSettings" );
            string certificateFileName = certificateSettings.GetValue<string> ( "filename" );
            string certificatePassword = certificateSettings.GetValue<string> ( "password" );

            var certificate = new X509Certificate2 ( certificateFileName, certificatePassword );

            var webApplicationOptions = new WebApplicationOptions ()
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory,
                ApplicationName = System.Diagnostics.Process.GetCurrentProcess ().ProcessName
            };

            var builder = WebApplication.CreateBuilder ( webApplicationOptions );

            builder.Host.UseWindowsService ();

            builder.WebHost.ConfigureKestrel (
                serverOptions =>
                {
                    serverOptions.ConfigureHttpsDefaults (
                        options =>
                        {
                            options.ClientCertificateMode = ClientCertificateMode.NoCertificate; // ClientCertificateMode.RequireCertificate;


                        } );

                } )
                .UseKestrel (
                    options =>
                    {
                        options.AddServerHeader = false;
                        options.Listen ( IPAddress.Loopback, ports.ApiPort, listenOptions =>
                        {
                            listenOptions.UseHttps ( certificate );
                        } );
                    } )
                .UseConfiguration ( config );




            // Add services to the container.

            builder.Services.AddControllersWithViews ();
            builder.Services.AddRazorPages ();

            // NLog: Setup NLog for Dependency injection
            builder.Logging.ClearProviders ();
            builder.Logging.SetMinimumLevel ( Microsoft.Extensions.Logging.LogLevel.Trace );
            builder.Host.UseNLog ();
            

            var app = builder.Build ();

            // Configure the HTTP request pipeline.
            if ( app.Environment.IsDevelopment () )
            {
                app.UseWebAssemblyDebugging ();
            }
            else
            {
                app.UseExceptionHandler ( "/Error" );
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts ();
            }

            app.UseHttpsRedirection ();

            app.UseBlazorFrameworkFiles ();
            app.UseStaticFiles ();

            app.UseRouting ();


            app.MapRazorPages ();
            app.MapControllers ();
            app.MapFallbackToFile ( "index.html" );

            app.Run ();
        }



    }
}