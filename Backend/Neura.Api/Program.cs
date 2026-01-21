using Hangfire;
using Neura.Api;
using Neura.Repository.Persistence;
using Scalar.AspNetCore;
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
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHangfireDashboard("/jops", new DashboardOptions
{
    //Authorization = [
    //    new HangfireCustomBasicAuthenticationFilter
    //    {
    //        User = app.Configuration.GetValue<string>("HangfireSettings:Username"),
    //        Pass = app.Configuration.GetValue<string>("HangfireSettings:Password")
    //    }
    //],
    DashboardTitle = "Neura"
});


var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();
var notificationService = scope.ServiceProvider.GetRequiredService<IUserService>();

RecurringJob.AddOrUpdate("SendNewPollsNotification", () => notificationService.SendMail(), "42 19 * * *");


#region ApplyPendingMigration

using var scopeApplicationContext = app.Services.CreateScope();
var context = scopeApplicationContext.ServiceProvider.GetRequiredService<ApplicationDbContext>();
try
{
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = scopeApplicationContext.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred while migrating the database.");
}

#endregion

app.UseSerilogRequestLogging();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.MapStaticAssets();

app.Run();