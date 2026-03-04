using MongoDB.Driver;
using RpgMongoDb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration["RpgDatabaseSettings:ConnectionString"];
var databaseName = builder.Configuration["RpgDatabaseSettings:DatabaseName"];

if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new Exception("Nedostaje ConnectionString ili DatabaseName u appsettings.json fajlu!");
}

builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddSingleton<AuctionService>();
builder.Services.AddSingleton<ClanService>();
builder.Services.AddSingleton<PlayerService>();
builder.Services.AddSingleton<ItemService>();
builder.Services.AddSingleton<LootBoxService>();

builder.Services.AddHostedService<RpgMongoDb.BackgroundServices.AuctionBackgroundService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.Run();