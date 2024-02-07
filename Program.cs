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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<VeggyContext>();

builder.Services.AddSingleton<LemmyService>();
builder.Services.AddSingleton<GotifyService>();
builder.Services.AddSingleton<StatusService>();
builder.Services.AddScoped<StorageService>();

WebApplication? app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run(); 