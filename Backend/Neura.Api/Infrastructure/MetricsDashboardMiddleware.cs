//namespace Neura.Api.Infrastructure;

///// <summary>
///// Serves a self-contained HTML metrics dashboard at /metrics/dashboard.
///// Auto-refreshes every 5 seconds by fetching /metrics/json.
///// </summary>
//public static class MetricsDashboardMiddleware
//{
//	public static void MapMetricsDashboard(this WebApplication app)
//	{
//		app.MapGet("/metrics/dashboard", () =>
//		{
//			var html = GetDashboardHtml();
//			return Results.Content(html, "text/html");
//		})
//		.ExcludeFromDescription();
//	}

//	private static string GetDashboardHtml() => """
//<!DOCTYPE html>
//<html lang="en">
//<head>
//<meta charset="UTF-8">
//<meta name="viewport" content="width=device-width, initial-scale=1.0">
//<title>Neura API — Metrics Dashboard</title>
//<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
//<style>
//  :root {
//    --bg-primary: #0f0f17;
//    --bg-card: #1a1a2e;
//    --bg-card-hover: #1e1e35;
//    --bg-table-header: #16162a;
//    --border: #2a2a4a;
//    --text-primary: #e8e8f0;
//    --text-secondary: #9898b8;
//    --text-muted: #6868a0;
//    --accent-purple: #7c3aed;
//    --accent-purple-light: #a78bfa;
//    --accent-blue: #3b82f6;
//    --accent-green: #10b981;
//    --accent-red: #ef4444;
//    --accent-amber: #f59e0b;
//    --accent-cyan: #06b6d4;
//    --gradient-purple: linear-gradient(135deg, #7c3aed, #a855f7);
//    --gradient-blue: linear-gradient(135deg, #3b82f6, #60a5fa);
//    --gradient-green: linear-gradient(135deg, #10b981, #34d399);
//    --gradient-red: linear-gradient(135deg, #ef4444, #f87171);
//    --shadow-glow: 0 0 20px rgba(124, 58, 237, 0.15);
//  }

//  * { margin: 0; padding: 0; box-sizing: border-box; }

//  body {
//    font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
//    background: var(--bg-primary);
//    color: var(--text-primary);
//    min-height: 100vh;
//  }

//  .dashboard {
//    max-width: 1400px;
//    margin: 0 auto;
//    padding: 24px;
//  }

//  /* ── Header ──────────────────────────────────────────── */
//  .header {
//    display: flex;
//    align-items: center;
//    justify-content: space-between;
//    margin-bottom: 32px;
//    padding-bottom: 24px;
//    border-bottom: 1px solid var(--border);
//  }

//  .header-left {
//    display: flex;
//    align-items: center;
//    gap: 16px;
//  }

//  .logo {
//    width: 44px;
//    height: 44px;
//    background: var(--gradient-purple);
//    border-radius: 12px;
//    display: flex;
//    align-items: center;
//    justify-content: center;
//    font-size: 20px;
//    font-weight: 700;
//    color: white;
//    box-shadow: var(--shadow-glow);
//  }

//  .header h1 {
//    font-size: 24px;
//    font-weight: 700;
//    background: linear-gradient(135deg, var(--text-primary), var(--accent-purple-light));
//    -webkit-background-clip: text;
//    -webkit-text-fill-color: transparent;
//  }

//  .header-subtitle {
//    font-size: 13px;
//    color: var(--text-muted);
//    margin-top: 2px;
//  }

//  .live-badge {
//    display: flex;
//    align-items: center;
//    gap: 8px;
//    padding: 8px 16px;
//    background: rgba(16, 185, 129, 0.1);
//    border: 1px solid rgba(16, 185, 129, 0.2);
//    border-radius: 20px;
//    font-size: 13px;
//    color: var(--accent-green);
//    font-weight: 500;
//  }

//  .live-dot {
//    width: 8px;
//    height: 8px;
//    background: var(--accent-green);
//    border-radius: 50%;
//    animation: pulse 2s infinite;
//  }

//  @keyframes pulse {
//    0%, 100% { opacity: 1; box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.4); }
//    50% { opacity: 0.7; box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
//  }

//  /* ── Stat Cards ──────────────────────────────────────── */
//  .stats-grid {
//    display: grid;
//    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
//    gap: 16px;
//    margin-bottom: 32px;
//  }

//  .stat-card {
//    background: var(--bg-card);
//    border: 1px solid var(--border);
//    border-radius: 16px;
//    padding: 24px;
//    position: relative;
//    overflow: hidden;
//    transition: transform 0.2s, border-color 0.2s;
//  }

//  .stat-card:hover {
//    transform: translateY(-2px);
//    border-color: rgba(124, 58, 237, 0.3);
//  }

//  .stat-card::before {
//    content: '';
//    position: absolute;
//    top: 0;
//    left: 0;
//    right: 0;
//    height: 3px;
//  }

//  .stat-card:nth-child(1)::before { background: var(--gradient-purple); }
//  .stat-card:nth-child(2)::before { background: var(--gradient-blue); }
//  .stat-card:nth-child(3)::before { background: var(--gradient-red); }
//  .stat-card:nth-child(4)::before { background: var(--gradient-green); }
//  .stat-card:nth-child(5)::before { background: linear-gradient(135deg, var(--accent-cyan), #22d3ee); }

//  .stat-label {
//    font-size: 12px;
//    font-weight: 500;
//    color: var(--text-muted);
//    text-transform: uppercase;
//    letter-spacing: 0.8px;
//    margin-bottom: 8px;
//  }

//  .stat-value {
//    font-size: 32px;
//    font-weight: 700;
//    line-height: 1;
//  }

//  .stat-card:nth-child(1) .stat-value { color: var(--accent-purple-light); }
//  .stat-card:nth-child(2) .stat-value { color: var(--accent-blue); }
//  .stat-card:nth-child(3) .stat-value { color: var(--accent-red); }
//  .stat-card:nth-child(4) .stat-value { color: var(--accent-green); }
//  .stat-card:nth-child(5) .stat-value { color: var(--accent-cyan); }

//  /* ── Table ───────────────────────────────────────────── */
//  .table-container {
//    background: var(--bg-card);
//    border: 1px solid var(--border);
//    border-radius: 16px;
//    overflow: hidden;
//  }

//  .table-header {
//    display: flex;
//    align-items: center;
//    justify-content: space-between;
//    padding: 20px 24px;
//    border-bottom: 1px solid var(--border);
//  }

//  .table-title {
//    font-size: 16px;
//    font-weight: 600;
//  }

//  .table-actions {
//    display: flex;
//    gap: 8px;
//    align-items: center;
//  }

//  .sort-btn {
//    padding: 6px 12px;
//    border-radius: 8px;
//    border: 1px solid var(--border);
//    background: transparent;
//    color: var(--text-secondary);
//    font-size: 12px;
//    font-family: 'Inter', sans-serif;
//    cursor: pointer;
//    transition: all 0.2s;
//  }

//  .sort-btn:hover, .sort-btn.active {
//    border-color: var(--accent-purple);
//    color: var(--accent-purple-light);
//    background: rgba(124, 58, 237, 0.1);
//  }

//  table {
//    width: 100%;
//    border-collapse: collapse;
//  }

//  thead th {
//    text-align: left;
//    padding: 14px 20px;
//    font-size: 11px;
//    font-weight: 600;
//    color: var(--text-muted);
//    text-transform: uppercase;
//    letter-spacing: 0.8px;
//    background: var(--bg-table-header);
//    border-bottom: 1px solid var(--border);
//    cursor: pointer;
//    user-select: none;
//    white-space: nowrap;
//  }

//  thead th:hover { color: var(--accent-purple-light); }

//  tbody tr {
//    border-bottom: 1px solid rgba(42, 42, 74, 0.5);
//    transition: background 0.15s;
//  }

//  tbody tr:hover { background: var(--bg-card-hover); }
//  tbody tr:last-child { border-bottom: none; }

//  td {
//    padding: 14px 20px;
//    font-size: 13px;
//    white-space: nowrap;
//  }

//  .method-badge {
//    display: inline-block;
//    padding: 3px 8px;
//    border-radius: 6px;
//    font-size: 11px;
//    font-weight: 600;
//    letter-spacing: 0.5px;
//  }

//  .method-GET { background: rgba(16, 185, 129, 0.15); color: var(--accent-green); }
//  .method-POST { background: rgba(59, 130, 246, 0.15); color: var(--accent-blue); }
//  .method-PUT { background: rgba(245, 158, 11, 0.15); color: var(--accent-amber); }
//  .method-PATCH { background: rgba(168, 85, 247, 0.15); color: var(--accent-purple-light); }
//  .method-DELETE { background: rgba(239, 68, 68, 0.15); color: var(--accent-red); }

//  .route { color: var(--text-secondary); font-family: 'SF Mono', 'Cascadia Code', monospace; font-size: 12px; }

//  .duration { font-weight: 600; font-variant-numeric: tabular-nums; }
//  .duration-fast { color: var(--accent-green); }
//  .duration-medium { color: var(--accent-amber); }
//  .duration-slow { color: var(--accent-red); }

//  .error-rate { font-weight: 600; }
//  .error-low { color: var(--accent-green); }
//  .error-medium { color: var(--accent-amber); }
//  .error-high { color: var(--accent-red); }

//  .count { color: var(--text-secondary); font-variant-numeric: tabular-nums; }

//  .status-codes {
//    display: flex;
//    gap: 4px;
//    flex-wrap: wrap;
//  }

//  .status-chip {
//    padding: 2px 6px;
//    border-radius: 4px;
//    font-size: 10px;
//    font-weight: 600;
//    font-variant-numeric: tabular-nums;
//  }

//  .status-2xx { background: rgba(16, 185, 129, 0.12); color: var(--accent-green); }
//  .status-3xx { background: rgba(6, 182, 212, 0.12); color: var(--accent-cyan); }
//  .status-4xx { background: rgba(245, 158, 11, 0.12); color: var(--accent-amber); }
//  .status-5xx { background: rgba(239, 68, 68, 0.12); color: var(--accent-red); }

//  /* ── Empty State ─────────────────────────────────────── */
//  .empty-state {
//    text-align: center;
//    padding: 80px 24px;
//    color: var(--text-muted);
//  }

//  .empty-state-icon {
//    font-size: 48px;
//    margin-bottom: 16px;
//    opacity: 0.5;
//  }

//  .empty-state h3 {
//    font-size: 18px;
//    color: var(--text-secondary);
//    margin-bottom: 8px;
//  }

//  .empty-state p { font-size: 14px; }

//  /* ── Last Updated ────────────────────────────────────── */
//  .last-updated {
//    text-align: center;
//    padding: 16px;
//    font-size: 12px;
//    color: var(--text-muted);
//  }

//  /* ── Responsive ──────────────────────────────────────── */
//  @media (max-width: 768px) {
//    .dashboard { padding: 16px; }
//    .header { flex-direction: column; gap: 16px; align-items: flex-start; }
//    .stats-grid { grid-template-columns: repeat(2, 1fr); }
//    .table-container { overflow-x: auto; }
//    table { min-width: 900px; }
//  }

//  /* ── Skeleton loading ────────────────────────────────── */
//  .skeleton {
//    background: linear-gradient(90deg, var(--bg-card) 25%, rgba(42, 42, 74, 0.8) 50%, var(--bg-card) 75%);
//    background-size: 200% 100%;
//    animation: shimmer 1.5s infinite;
//    border-radius: 4px;
//  }

//  @keyframes shimmer {
//    0% { background-position: 200% 0; }
//    100% { background-position: -200% 0; }
//  }
//</style>
//</head>
//<body>
//<div class="dashboard">
//  <!-- Header -->
//  <div class="header">
//    <div class="header-left">
//      <div class="logo">N</div>
//      <div>
//        <h1>Neura API Metrics</h1>
//        <div class="header-subtitle">Real-time endpoint performance monitoring</div>
//      </div>
//    </div>
//    <div class="live-badge">
//      <div class="live-dot"></div>
//      <span>Live — refreshing every 5s</span>
//    </div>
//  </div>

//  <!-- Stats -->
//  <div class="stats-grid" id="statsGrid">
//    <div class="stat-card">
//      <div class="stat-label">Total Requests</div>
//      <div class="stat-value" id="totalRequests">—</div>
//    </div>
//    <div class="stat-card">
//      <div class="stat-label">Endpoints Tracked</div>
//      <div class="stat-value" id="totalEndpoints">—</div>
//    </div>
//    <div class="stat-card">
//      <div class="stat-label">Total Errors</div>
//      <div class="stat-value" id="totalErrors">—</div>
//    </div>
//    <div class="stat-card">
//      <div class="stat-label">Error Rate</div>
//      <div class="stat-value" id="errorRate">—</div>
//    </div>
//    <div class="stat-card">
//      <div class="stat-label">Active Requests</div>
//      <div class="stat-value" id="activeRequests">—</div>
//    </div>
//  </div>

//  <!-- Table -->
//  <div class="table-container">
//    <div class="table-header">
//      <div class="table-title">Endpoint Performance</div>
//      <div class="table-actions">
//        <button class="sort-btn active" data-sort="requests" onclick="sortBy('requests')">By Requests</button>
//        <button class="sort-btn" data-sort="avg" onclick="sortBy('avg')">By Avg Time</button>
//        <button class="sort-btn" data-sort="p95" onclick="sortBy('p95')">By P95</button>
//        <button class="sort-btn" data-sort="errors" onclick="sortBy('errors')">By Errors</button>
//      </div>
//    </div>
//    <div id="tableContent">
//      <div class="empty-state">
//        <div class="empty-state-icon">📊</div>
//        <h3>Waiting for data...</h3>
//        <p>Make some API requests and metrics will appear here automatically.</p>
//      </div>
//    </div>
//  </div>

//  <div class="last-updated" id="lastUpdated"></div>
//</div>

//<script>
//let currentSort = 'requests';
//let metricsData = null;

//function durationClass(ms) {
//  if (ms < 100) return 'duration-fast';
//  if (ms < 500) return 'duration-medium';
//  return 'duration-slow';
//}

//function errorClass(rate) {
//  if (rate < 1) return 'error-low';
//  if (rate < 5) return 'error-medium';
//  return 'error-high';
//}

//function statusClass(code) {
//  if (code < 300) return 'status-2xx';
//  if (code < 400) return 'status-3xx';
//  if (code < 500) return 'status-4xx';
//  return 'status-5xx';
//}

//function formatNum(n) {
//  if (n >= 1000000) return (n / 1000000).toFixed(1) + 'M';
//  if (n >= 1000) return (n / 1000).toFixed(1) + 'K';
//  return n.toString();
//}

//function sortBy(field) {
//  currentSort = field;
//  document.querySelectorAll('.sort-btn').forEach(b => b.classList.remove('active'));
//  document.querySelector(`[data-sort="${field}"]`).classList.add('active');
//  if (metricsData) render(metricsData);
//}

//function render(data) {
//  metricsData = data;

//  document.getElementById('totalRequests').textContent = formatNum(data.totalRequests);
//  document.getElementById('totalEndpoints').textContent = data.totalEndpoints;
//  document.getElementById('totalErrors').textContent = formatNum(data.totalErrors);
//  document.getElementById('errorRate').textContent = data.overallErrorRate + '%';
//  document.getElementById('activeRequests').textContent = data.activeRequests;

//  if (!data.endpoints || data.endpoints.length === 0) return;

//  let endpoints = [...data.endpoints];
//  switch (currentSort) {
//    case 'requests': endpoints.sort((a, b) => b.totalRequests - a.totalRequests); break;
//    case 'avg': endpoints.sort((a, b) => b.avgDurationMs - a.avgDurationMs); break;
//    case 'p95': endpoints.sort((a, b) => b.p95DurationMs - a.p95DurationMs); break;
//    case 'errors': endpoints.sort((a, b) => b.errorCount - a.errorCount); break;
//  }

//  let html = `<table>
//    <thead>
//      <tr>
//        <th>Method</th>
//        <th>Route</th>
//        <th>Requests</th>
//        <th>Avg</th>
//        <th>P50</th>
//        <th>P95</th>
//        <th>P99</th>
//        <th>Min</th>
//        <th>Max</th>
//        <th>Errors</th>
//        <th>Error %</th>
//        <th>Status Codes</th>
//      </tr>
//    </thead>
//    <tbody>`;

//  for (const ep of endpoints) {
//    const statusChips = Object.entries(ep.statusCodeCounts)
//      .sort(([a], [b]) => Number(a) - Number(b))
//      .map(([code, count]) => `<span class="status-chip ${statusClass(Number(code))}">${code}: ${count}</span>`)
//      .join('');

//    html += `<tr>
//      <td><span class="method-badge method-${ep.method}">${ep.method}</span></td>
//      <td class="route">${ep.route}</td>
//      <td class="count">${formatNum(ep.totalRequests)}</td>
//      <td class="duration ${durationClass(ep.avgDurationMs)}">${ep.avgDurationMs}ms</td>
//      <td class="duration ${durationClass(ep.p50DurationMs)}">${ep.p50DurationMs}ms</td>
//      <td class="duration ${durationClass(ep.p95DurationMs)}">${ep.p95DurationMs}ms</td>
//      <td class="duration ${durationClass(ep.p99DurationMs)}">${ep.p99DurationMs}ms</td>
//      <td class="duration ${durationClass(ep.minDurationMs)}">${ep.minDurationMs}ms</td>
//      <td class="duration ${durationClass(ep.maxDurationMs)}">${ep.maxDurationMs}ms</td>
//      <td class="count">${ep.errorCount}</td>
//      <td class="error-rate ${errorClass(ep.errorRate)}">${ep.errorRate.toFixed(1)}%</td>
//      <td><div class="status-codes">${statusChips}</div></td>
//    </tr>`;
//  }

//  html += '</tbody></table>';
//  document.getElementById('tableContent').innerHTML = html;
//  document.getElementById('lastUpdated').textContent =
//    'Last updated: ' + new Date(data.timestamp).toLocaleTimeString();
//}

//async function fetchMetrics() {
//  try {
//    const res = await fetch('/metrics/json');
//    const data = await res.json();
//    render(data);
//  } catch (e) {
//    console.warn('Failed to fetch metrics:', e);
//  }
//}

//fetchMetrics();
//setInterval(fetchMetrics, 5000);
//</script>
//</body>
//</html>
//""";
//}
