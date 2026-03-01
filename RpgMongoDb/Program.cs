using RpgMongoDb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration["RpgDatabaseSettings:ConnectionString"];
var databaseName = builder.Configuration["RpgDatabaseSettings:DatabaseName"];

if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new Exception("Nedostaje ConnectionString ili DatabaseName u appsettings.json fajlu!");
}

// Registracija tvojih servisa
builder.Services.AddSingleton(new MongoDbService(connectionString, databaseName));
builder.Services.AddSingleton(new ClanService(connectionString, databaseName));

// 1. Klasičan Swagger setup (Obrisano AddOpenApi)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // 2. Aktiviranje starog dobrog Swagger interfejsa
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();