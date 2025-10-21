using GraduationProject.Api;
using GraduationProject.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDependencies(builder.Configuration);

builder.Host.UseSerilog((context, configuration) =>
	configuration.ReadFrom.Configuration(context.Configuration)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

//#region ApplyPendingMigration

//using var scopeApplicationContext = app.Services.CreateScope();
//var context = scopeApplicationContext.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//try
//{
//	await context.Database.MigrateAsync();
//}
//catch (Exception e)
//{
//	var logger = scopeApplicationContext.ServiceProvider.GetRequiredService<ILogger<Program>>();
//	logger.LogError(e, "An error occurred while migrating the database.");
//}

//#endregion

app.UseSerilogRequestLogging();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.Run();