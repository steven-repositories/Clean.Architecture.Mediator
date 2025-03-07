using Clean.Architecture.Mediator.API;
using Clean.Architecture.Mediator.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ConfigureConfiguration();
builder.Configuration.ConfigureHost(builder.Host);
builder.Configuration.ConfigureServices(builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Middlewares here

app.MapControllers();

using var scope = app
    .Services
    .CreateScope();

scope
    .ServiceProvider
    .GetRequiredService<ApplicationDbContext>()
    .Database
    .MigrateAsync()
    .GetAwaiter()
    .GetResult();

app.Run();
