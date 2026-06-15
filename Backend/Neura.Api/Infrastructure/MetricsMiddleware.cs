//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.Diagnostics.Metrics;

//namespace Neura.Api.Infrastructure;

///// <summary>
///// Middleware that collects per-request metrics: response time, request count,
///// error count, and active (in-flight) requests. Data is stored in-memory for
///// the built-in dashboard and also published via OpenTelemetry instruments.
///// </summary>
//public sealed class MetricsMiddleware
//{
//	private readonly RequestDelegate _next;

//	// ── OpenTelemetry instruments ────────────────────────────────────────
//	private static readonly Meter Meter = new("Neura.Api", "1.0.0");

//	private static readonly Histogram<double> RequestDuration =
//		Meter.CreateHistogram<double>(
//			"neura.http.request.duration",
//			unit: "ms",
//			description: "HTTP request duration in milliseconds");

//	private static readonly Counter<long> RequestCount =
//		Meter.CreateCounter<long>(
//			"neura.http.request.count",
//			description: "Total HTTP request count");

//	private static readonly Counter<long> ErrorCount =
//		Meter.CreateCounter<long>(
//			"neura.http.request.errors",
//			description: "Total HTTP error count (4xx/5xx)");

//	private static readonly UpDownCounter<long> ActiveRequests =
//		Meter.CreateUpDownCounter<long>(
//			"neura.http.active_requests",
//			description: "Currently active HTTP requests");

//	// ── In-memory storage for the dashboard ──────────────────────────────
//	public static readonly ConcurrentDictionary<string, EndpointMetrics> EndpointData = new();
//	public static long TotalActiveRequests;

//	public MetricsMiddleware(RequestDelegate next)
//	{
//		_next = next;
//	}

//	public async Task InvokeAsync(HttpContext context)
//	{
//		// Skip metrics/health/swagger requests
//		var path = context.Request.Path.Value ?? "";
//		if (path.StartsWith("/metrics") ||
//			path.StartsWith("/health") ||
//			path.StartsWith("/openapi") ||
//			path.StartsWith("/scalar") ||
//			path.StartsWith("/swagger") ||
//			path.StartsWith("/jobs"))
//		{
//			await _next(context);
//			return;
//		}

//		var sw = Stopwatch.StartNew();
//		Interlocked.Increment(ref TotalActiveRequests);
//		ActiveRequests.Add(1);

//		try
//		{
//			await _next(context);
//		}
//		finally
//		{
//			sw.Stop();
//			Interlocked.Decrement(ref TotalActiveRequests);
//			ActiveRequests.Add(-1);

//			var method = context.Request.Method;
//			var route = GetRoute(context);
//			var statusCode = context.Response.StatusCode;
//			var durationMs = sw.Elapsed.TotalMilliseconds;

//			var tags = new TagList
//			{
//				{ "http.method", method },
//				{ "http.route", route },
//				{ "http.status_code", statusCode }
//			};

//			RequestDuration.Record(durationMs, tags);
//			RequestCount.Add(1, tags);

//			if (statusCode >= 400)
//				ErrorCount.Add(1, tags);

//			// Store in-memory for the dashboard
//			var key = $"{method} {route}";
//			var metrics = EndpointData.GetOrAdd(key, _ => new EndpointMetrics(method, route));
//			metrics.Record(durationMs, statusCode);
//		}
//	}

//	private static string GetRoute(HttpContext context)
//	{
//		var endpoint = context.GetEndpoint();
//		if (endpoint is RouteEndpoint routeEndpoint)
//			return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";

//		return context.Request.Path.Value ?? "/";
//	}
//}

///// <summary>
///// Thread-safe per-endpoint metrics accumulator.
///// Stores a sliding window of the last 1000 request durations for percentile calculation.
///// </summary>
//public sealed class EndpointMetrics
//{
//	public string Method { get; }
//	public string Route { get; }

//	private long _totalRequests;
//	private long _errorCount;
//	private double _totalDuration;
//	private double _minDuration = double.MaxValue;
//	private double _maxDuration;

//	// Sliding window for percentile calculations (last 1000 requests)
//	private readonly double[] _recentDurations = new double[1000];
//	private long _durationIndex;

//	private readonly ConcurrentDictionary<int, long> _statusCodeCounts = new();

//	public EndpointMetrics(string method, string route)
//	{
//		Method = method;
//		Route = route;
//	}

//	public void Record(double durationMs, int statusCode)
//	{
//		Interlocked.Increment(ref _totalRequests);

//		// Thread-safe min/max updates
//		double currentMin, currentMax;
//		do { currentMin = _minDuration; }
//		while (durationMs < currentMin &&
//			   Interlocked.CompareExchange(ref _minDuration, durationMs, currentMin) != currentMin);

//		do { currentMax = _maxDuration; }
//		while (durationMs > currentMax &&
//			   Interlocked.CompareExchange(ref _maxDuration, durationMs, currentMax) != currentMax);

//		// Relaxed addition (tiny drift is acceptable for dashboards)
//		var newTotal = _totalDuration + durationMs;
//		Interlocked.Exchange(ref _totalDuration, newTotal);

//		// Sliding window
//		var idx = Interlocked.Increment(ref _durationIndex) - 1;
//		_recentDurations[idx % _recentDurations.Length] = durationMs;

//		if (statusCode >= 400)
//			Interlocked.Increment(ref _errorCount);

//		_statusCodeCounts.AddOrUpdate(statusCode, 1, (_, count) => count + 1);
//	}

//	public EndpointSnapshot GetSnapshot()
//	{
//		var total = Interlocked.Read(ref _totalRequests);
//		var errors = Interlocked.Read(ref _errorCount);
//		var count = Math.Min(total, _recentDurations.Length);

//		double avg = 0, p50 = 0, p95 = 0, p99 = 0;

//		if (count > 0)
//		{
//			var sorted = new double[count];
//			Array.Copy(_recentDurations, sorted, count);
//			Array.Sort(sorted);

//			avg = _totalDuration / total;
//			p50 = sorted[(int)(count * 0.50)];
//			p95 = sorted[(int)(count * 0.95)];
//			p99 = sorted[(int)Math.Min(count * 0.99, count - 1)];
//		}

//		return new EndpointSnapshot
//		{
//			Method = Method,
//			Route = Route,
//			TotalRequests = total,
//			ErrorCount = errors,
//			ErrorRate = total > 0 ? (double)errors / total * 100 : 0,
//			AvgDurationMs = Math.Round(avg, 2),
//			MinDurationMs = Math.Round(_minDuration == double.MaxValue ? 0 : _minDuration, 2),
//			MaxDurationMs = Math.Round(_maxDuration, 2),
//			P50DurationMs = Math.Round(p50, 2),
//			P95DurationMs = Math.Round(p95, 2),
//			P99DurationMs = Math.Round(p99, 2),
//			StatusCodeCounts = new Dictionary<int, long>(_statusCodeCounts)
//		};
//	}
//}

//public sealed class EndpointSnapshot
//{
//	public string Method { get; set; } = "";
//	public string Route { get; set; } = "";
//	public long TotalRequests { get; set; }
//	public long ErrorCount { get; set; }
//	public double ErrorRate { get; set; }
//	public double AvgDurationMs { get; set; }
//	public double MinDurationMs { get; set; }
//	public double MaxDurationMs { get; set; }
//	public double P50DurationMs { get; set; }
//	public double P95DurationMs { get; set; }
//	public double P99DurationMs { get; set; }
//	public Dictionary<int, long> StatusCodeCounts { get; set; } = new();
//}
