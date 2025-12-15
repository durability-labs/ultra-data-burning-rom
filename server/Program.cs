using UltraDataBurningROM.Server;
using UltraDataBurningROM.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Initialize the storage immediately.
var storageService = new StorageService();
storageService.Initialize();

// We use the configured volume size as the max request size.
long maxRequestBodySize = Convert.ToInt64(EnvConfig.VolumeSize);
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = maxRequestBodySize; });

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IStorageService>(storageService);
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IBucketService, BucketService>();
builder.Services.AddSingleton<IMountService, MountService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IBurnService, BurnService>();
builder.Services.AddSingleton<IPopularContentService, PopularContentService>();
builder.Services.AddSingleton<IWorkerService, WorkerService>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<IMapperService, MapperService>();
builder.Services.AddSingleton<ICleanupService, CleanupService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

var popularContentService = app.Services.GetService<IPopularContentService>()!;
var searchService = app.Services.GetService<ISearchService>()!;
var cleanupService = app.Services.GetService<ICleanupService>()!;
popularContentService.Start();
searchService.Start();
cleanupService.Start();

var workerService = app.Services.GetService<IWorkerService>()!;
workerService.LateStart();

app.Run();

workerService.Stop();
