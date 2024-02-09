global using System;
global using System.ComponentModel.DataAnnotations;
global using System.IO;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Collections.Specialized;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq;
global using System.Linq.Dynamic.Core;
global using System.Net;
global using System.Net.Http;
global using System.Numerics;
global using System.Reflection;
global using System.Security.Authentication;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Web;
global using System.Xml.Linq;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.RazorPages;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.ChangeTracking;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.JSInterop;

global using Npgsql;
global using RestSharp;
global using RestSharp.Authenticators;
global using WatsonWebsocket;

global using Veggy.Enums;
global using Veggy.Exceptions;
global using Veggy.Extensions;
global using Veggy.Models;
global using Veggy.Models.Lemmy;
global using Veggy.Models.Gotify;
global using Veggy.Pages;
global using Veggy.Shared;
global using Veggy.Services;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

// Add services to the container.
webApplicationBuilder.Services.AddRazorPages();
webApplicationBuilder.Services.AddServerSideBlazor();

webApplicationBuilder.Services.AddDbContextFactory<VeggyContext>();

webApplicationBuilder.Services.AddSingleton<LemmyService>();
webApplicationBuilder.Services.AddSingleton<GotifyService>();
webApplicationBuilder.Services.AddSingleton<StatusService>();
webApplicationBuilder.Services.AddScoped<StorageService>();
webApplicationBuilder.Services.AddHostedService<VeggyHostedService>();

WebApplication webApplication = webApplicationBuilder.Build();

using (IServiceScope scope = webApplication.Services.CreateAsyncScope())
{
    VeggyContext veggyContext = scope.ServiceProvider.GetRequiredService<VeggyContext>();
    
    veggyContext.Database.Migrate();
    await veggyContext.Settings.PopulateDefaultsAsync();
}

// Configure the HTTP request pipeline.
if (!webApplication.Environment.IsDevelopment())
{
    webApplication.UseDeveloperExceptionPage();
}
else
{
    webApplication.UseExceptionHandler("/Error");
}

webApplication.UseStaticFiles();

webApplication.UseRouting();

webApplication.MapControllers();
webApplication.MapBlazorHub();
webApplication.MapFallbackToPage("/_Host");

webApplication.Run(); 