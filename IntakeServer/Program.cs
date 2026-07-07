using IntakeServer;
using IntakeServer.Middleware;
using IntakeServer.Repositories.Invoices;
using IntakeServer.Services.Invoices;
using LibDataAccess;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<IntakeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapper-based data access (LibDataAccess) available for future repositories.
builder.Services.AddTransient<IDbConnection>(sp =>
    new NpgsqlConnection(connectionString));
builder.Services.AddTransient<IDataAccess, DataAccess>();

builder.Services.AddControllers();

// Invoice intake: Controller -> Service -> Repository -> DbContext
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<IntakeDbContext>("database");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7236", "http://localhost:5071")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorClient");
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
