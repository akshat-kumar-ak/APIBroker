using APIBroker.Interfaces;
using APIBroker.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IApiBrokerService, ApiBrokerService>();
builder.Services.AddScoped<IProviderBrokerService, ProviderBrokerService>();

// Redis Cache Service
var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration["RedisCacheSettings:ConnectionString"]);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Configure StackExchange Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisCacheSettings:ConnectionString"];
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
