using UltraDataBurningROM.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// We use the configured volume size as the max request size.
var envVar = Environment.GetEnvironmentVariable("BROM_ROMVOLUMESIZE");
if (string.IsNullOrEmpty(envVar)) throw new Exception("Missing environment variable: BROM_ROMVOLUMESIZE");
long maxRequestBodySize = Convert.ToInt64(envVar);
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = maxRequestBodySize; });

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IBucketService, BucketService>();
builder.Services.AddSingleton<IMountService, MountService>();
builder.Services.AddSingleton<IUserService, UserService>();

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

app.Run();
