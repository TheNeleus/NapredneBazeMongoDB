using RpgMongoDb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration["RpgDatabaseSettings:ConnectionString"];
var databaseName = builder.Configuration["RpgDatabaseSettings:DatabaseName"];

if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new Exception("Nedostaje ConnectionString ili DatabaseName u appsettings.json fajlu!");
}

builder.Services.AddSingleton(new AuctionService(connectionString, databaseName));
builder.Services.AddSingleton(new ClanService(connectionString, databaseName));
builder.Services.AddSingleton(new PlayerService(connectionString, databaseName));
builder.Services.AddSingleton(new ItemService(connectionString, databaseName));
builder.Services.AddSingleton(new RpgMongoDb.Services.LootBoxService(connectionString, databaseName));
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