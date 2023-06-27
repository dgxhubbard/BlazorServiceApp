using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


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
using System.Text;

namespace BlazorServiceApp.Server
{

    //
    // Refer to launchSettings.json
    // https://stackoverflow.com/questions/51738893/removing-kestrel-binding-warning
    //
    public class Program
    {
        #region Constants

        public const string Url =  "<%Url%>";
        public const string CertFilename = "<%CertFilename%>";
        public const string CertPassword = "<%CertPassword%>";

        #endregion




        public static void Main ( string [] args )
        {
            var path = Path.GetDirectoryName ( Assembly.GetExecutingAssembly ().Location );
            Directory.SetCurrentDirectory ( path );

            var logger = LogManager.Setup ().LoadConfigurationFromFile ( "nlog.config" ).GetCurrentClassLogger ();

            // set url to listen on
            // using ports.json for port
            // this is so user can specify port

            if ( !Ports.Exists ( path ) )
                Ports.SavePorts ( path );
            var ports = Ports.LoadPorts ( path );

            var url = "https://localhost"  + ":" + ports.ApiPort;

            // get certificate to use
            // using certificate.json for info
            // this so user can use their own certificate

            var config = new ConfigurationBuilder ()
                        .SetBasePath ( Directory.GetCurrentDirectory () )
                        .AddEnvironmentVariables ()
                        .AddJsonFile ( "certificate.json", optional: true, reloadOnChange: true )
                        .AddJsonFile ( $"certificate.{Environment.GetEnvironmentVariable ( "ASPNETCORE_ENVIRONMENT" )}.json", optional: true, reloadOnChange: true )
                        .Build ();

            var certificateSettings = config.GetSection ( "certificateSettings" );

            string certificateFilename = certificateSettings.GetValue<string> ( "filename" );
            string certificatePassword = certificateSettings.GetValue<string> ( "password" );


            //var certificate = new X509Certificate2 ( certificateFilename, certificatePassword );

            // replace app settings with url and certificate info
            var appSettingsCnst =
                @"
                    {
                      ""Logging"": {
                        ""LogLevel"": {
                          ""Default"": ""Information"",
                          ""Microsoft.AspNetCore"": ""Warning""
                        }
                      },
                      ""Kestrel"": {
                        ""Endpoints"": {
                          ""HttpsInlineCertFile"": {
                            ""Url"": ""<%Url%>"",
                            ""Certificate"": {
                              ""Path"": ""<%CertFilename%>"",
                              ""Password"": ""<%CertPassword%>""
                            }
                          }
                        }
                      },
                      ""AllowedHosts"": ""*""
                    }
                 ";


            var bldr = new StringBuilder ( appSettingsCnst );

            bldr.Replace ( Url, url );
            bldr.Replace ( CertFilename, certificateFilename );
            bldr.Replace ( CertPassword, certificatePassword );

            var appSettingsPath = Path.Combine ( path, "appsettings.json" );
            if ( File.Exists ( appSettingsPath ) )
                File.Delete ( appSettingsPath );

            File.WriteAllText ( Path.Combine ( path, "appsettings.json" ), bldr.ToString () );



            var builder = WebApplication.CreateBuilder ( new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService () ? AppContext.BaseDirectory : path
            } );

            builder.Host.UseWindowsService ();

            builder.Services.AddWindowsService ( options =>
            {
                options.ServiceName = "api";
            } );

            builder.WebHost.UseKestrel ( ( context, serverOptions ) =>
            {
                serverOptions.Configure ( context.Configuration.GetSection ( "Kestrel" ) )
                .Endpoint ( "HTTPS", listenOptions =>
                {
                    listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
                } );
            } );


            /*
            builder.WebHost.ConfigureKestrel (
                serverOptions =>
                {
                    serverOptions.ConfigureHttpsDefaults (
                        options =>
                        {
                            options.ClientCertificateMode = ClientCertificateMode.NoCertificate; // ClientCertificateMode.RequireCertificate;


                        } );

                } );
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
            */



            // Add services to the container.

            builder.Services.AddControllersWithViews ();
            builder.Services.AddRazorPages ();

            /*
            builder.Services.AddMvc ( options =>
            {
                options.SslPort = 7224;
                options.Filters.Add ( typeof ( RequireHttpsAttribute ) );
            } );
            */

            builder.Services.AddAntiforgery (
                options =>
                {
                    options.Cookie.Name = "_af";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.HeaderName = "X-XSRF-TOKEN";
                }
            );


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