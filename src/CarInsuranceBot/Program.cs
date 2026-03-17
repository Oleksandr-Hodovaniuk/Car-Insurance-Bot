using CarInsuranceBot;
using CarInsuranceBot.Application;
using CarInsuranceBot.Infrastructure;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load data from .env file.
//Env.Load("../../.env");   uncomment for local start up
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();