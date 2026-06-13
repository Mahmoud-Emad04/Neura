using Hangfire;
using Microsoft.Extensions.FileProviders;
using Neura.Api;
using Neura.Api.Infrastructure;
using Neura.Repository.Persistence;
using Neura.Services.Hubs;
using Neura.Services.Jobs;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDependencies(builder.Configuration, builder.Environment);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

//if (app.Environment.IsDevelopment())
//{
app.UseSwagger(options => { options.RouteTemplate = "openapi/{documentName}.json"; });
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Neura API")
        .WithTheme(ScalarTheme.Purple)
        .WithClassicLayout()
        .WithOpenApiRoutePattern("/openapi/v1.json");
});
//}

app.UseHangfireDashboard("/jobs", new DashboardOptions
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

RecurringJob.AddOrUpdate<ExamTimeoutJob>(
    recurringJobId: "exam-timeout-processor",
    methodCall: job => job.ExecuteAsync(),
    cronExpression: "*/1 * * * *"
);

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

app.UseExceptionHandler();

// Enable request body buffering for webhook endpoints (required for HMAC-SHA256 signature validation)
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/webhooks"),
    appBuilder => appBuilder.Use(async (context, next) =>
    {
        context.Request.EnableBuffering();
        await next();
    })
);

app.UseSerilogRequestLogging();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapEndpoints();

app.MapHub<CommunityHub>("/hubs/community");

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "Images")),
    RequestPath = "/Images"
});

app.Run();