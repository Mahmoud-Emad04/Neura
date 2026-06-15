//using System.Text.Json;

//namespace Neura.Api.Infrastructure;

///// <summary>
///// Serves collected metrics as JSON at /metrics/json.
///// Consumed by the built-in dashboard and available for external tools.
///// </summary>
//public static class MetricsJsonEndpoint
//{
//	private static readonly JsonSerializerOptions JsonOptions = new()
//	{
//		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//		WriteIndented = true
//	};

//	public static void MapMetricsJsonEndpoint(this WebApplication app)
//	{
//		app.MapGet("/metrics/json", () =>
//		{
//			var snapshots = MetricsMiddleware.EndpointData.Values
//				.Select(e => e.GetSnapshot())
//				.OrderByDescending(s => s.TotalRequests)
//				.ToList();

//			var response = new
//			{
//				Timestamp = DateTime.UtcNow,
//				ActiveRequests = Interlocked.Read(ref MetricsMiddleware.TotalActiveRequests),
//				TotalEndpoints = snapshots.Count,
//				TotalRequests = snapshots.Sum(s => s.TotalRequests),
//				TotalErrors = snapshots.Sum(s => s.ErrorCount),
//				OverallErrorRate = snapshots.Sum(s => s.TotalRequests) > 0
//					? Math.Round((double)snapshots.Sum(s => s.ErrorCount) / snapshots.Sum(s => s.TotalRequests) * 100, 2)
//					: 0,
//				Endpoints = snapshots
//			};

//			return Results.Json(response, JsonOptions);
//		})
//		.WithTags("Metrics")
//		.WithSummary("Returns all endpoint metrics as JSON")
//		.ExcludeFromDescription();
//	}
//}
