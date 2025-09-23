using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDbTutorial.Hubs;
using MongoDbTutorial.Models;
using MongoDbTutorial.Services;

var builder = WebApplication.CreateBuilder(args);

var mongoDbSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDbSettings>();
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDBSettings"));

// Services
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.AtlasURI);
});

builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();   // session before authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapHub<ChatHub>("/chathub");
app.Run();
