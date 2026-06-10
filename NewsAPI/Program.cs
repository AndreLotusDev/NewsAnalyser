using Hangfire;
using Hangfire.MemoryStorage;
using LiteDB;
using Microsoft.Extensions.FileProviders;
using NewsAPI.Infrastructure;
using NewsAPI.Jobs;
using NewsAPI.Repositories;
using NewsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5000", "https://localhost:5001"];

builder.Services.AddCors(options =>
    options.AddPolicy("MarketPulse", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var dbPath = builder.Configuration["LiteDb:Path"] ?? "./data/news.db";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);
var connectionString = new ConnectionString(dbPath) { Connection = ConnectionType.Shared };
builder.Services.AddSingleton(new LiteDatabase(connectionString));
builder.Services.AddSingleton<LiteDbContext>();

builder.Services.AddSingleton<CategoryRepository>();
builder.Services.AddSingleton<PostRepository>();
builder.Services.AddSingleton<AccountRepository>();
builder.Services.AddSingleton<JobStatusTracker>();
builder.Services.AddScoped<SocialService>();
builder.Services.AddHttpClient<ITwitterApiClient, TwitterApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.twitterapi.io/");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["TwitterApi:ApiKey"] ?? "");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IAiEnrichmentService, AiEnrichmentService>();
builder.Services.AddScoped<FetchPostsJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MarketPulse");
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Template", "fila", "assets")),
    RequestPath = "/fila-assets"
});

app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=News}/{action=Index}/{id?}");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireBasicAuthFilter("admin", "admin")]
});

RecurringJob.AddOrUpdate<FetchPostsJob>("fetch-posts", j => j.RunAsync(), "*/5 * * * *");

// Seed categories table from posts already in the database
var dbCtx = app.Services.GetRequiredService<LiteDbContext>();
var catRepo = app.Services.GetRequiredService<CategoryRepository>();
var seedCats = dbCtx.Posts.Query().ToList()
    .SelectMany(p => p.Categories ?? [])
    .Where(c => !string.IsNullOrWhiteSpace(c))
    .Distinct(StringComparer.OrdinalIgnoreCase);
catRepo.Sync(seedCats);

var accountRepo = app.Services.GetRequiredService<AccountRepository>();
if (accountRepo.GetAll().Count == 0)
{
    string[] seedHandles =
    [
        "OilandGibbs", "FirstSquawk", "madorni", "bsims1977", "EzazAhmadA_E",
        "palmthetrader", "MarsOleochem", "Saveraaintl", "benjaminbodart", "PalmOils",
        "biofuelslaw", "Goodvib75002247", "mgbongio", "uscanola", "SoybeanTrader88",
        "Ochefedoboss1", "lili_agri", "gaurav_kochar", "Biokraftstoff", "VisioCrop",
        "DutchFarmerInUA", "BiobasedDiesel", "EctTan", "JarrettRenshaw", "StephanieKellyM",
        "anilbagani", "PFLPetroleum", "LingamSupraman2", "GrainsGorilla", "sizov_andre",
        "EduardoVanin4", "DDFalpha", "FEDIOL_EU", "DeItaone", "ScottIrwinUI",
        "ArlanFF101", "FarmPolicy", "GRAINSOILSEEDS", "tx_marcelo", "agtradertalk",
        "agturbobrazil", "kannbwx", "realdonaldtrump", "zerohedge"
    ];

    var seedTime = DateTime.UtcNow;
    foreach (var handle in seedHandles)
        accountRepo.AddWithTimestamp(handle, seedTime);

    BackgroundJob.Enqueue<FetchPostsJob>(j => j.RunAsync());
}

app.Run();
