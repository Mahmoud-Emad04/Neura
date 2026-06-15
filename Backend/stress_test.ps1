<#
.SYNOPSIS
    Neura API - Stress Test and Metrics Report
.DESCRIPTION
    Hits all public GET endpoints with configurable concurrency and iterations,
    then fetches the metrics dashboard JSON and prints a formatted report.
#>

param(
    [string]$BaseUrl = "http://localhost:5017",
    [int]$Iterations = 20,
    [int]$Concurrency = 5
)

$ErrorActionPreference = "Continue"

function Write-Header($text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }
function Write-SubHeader($text) { Write-Host "  > $text" -ForegroundColor Yellow }
function Write-OK($text) { Write-Host "    [OK] $text" -ForegroundColor Green }
function Write-Err($text) { Write-Host "    [ERR] $text" -ForegroundColor Red }
function Write-Info($text) { Write-Host "    $text" -ForegroundColor Gray }

# Endpoints to test
$publicEndpoints = @(
    @{ Method = "GET";  Path = "/api/Courses" },
    @{ Method = "GET";  Path = "/api/Courses/full-content" },
    @{ Method = "GET";  Path = "/api/Tags/active" },
    @{ Method = "GET";  Path = "/api/Tags/popular" },
    @{ Method = "GET";  Path = "/api/Tags" },
    @{ Method = "GET";  Path = "/openapi/v1.json" }
)

$authEndpoints = @(
    @{ Method = "GET";  Path = "/api/Courses/bookmarked" },
    @{ Method = "GET";  Path = "/api/Courses/my/editable" },
    @{ Method = "GET";  Path = "/api/invitations/my" },
    @{ Method = "GET";  Path = "/api/instructor/application" },
    @{ Method = "GET";  Path = "/api/instructor/applications" }
)

$postEndpoints = @(
    @{ Method = "POST"; Path = "/Auth/login"; Body = @{ email = "stress-test@neura.dev"; password = "FakePassword123!" } },
    @{ Method = "POST"; Path = "/Auth/login"; Body = @{ email = "invalid@test.com"; password = "wrong" } }
)

# Step 1: Try login
Write-Header "STEP 1: Authentication"
$token = $null
try {
    $loginBody = @{ email = "m@m.com"; password = "Mahmoud@123" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/Auth/login" -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
    if ($loginResponse.token) {
        $token = $loginResponse.token
        Write-OK "Logged in successfully - token acquired"
    }
} catch {
    Write-Info "Could not login with test account (auth endpoints will get 401s)"
}

# Stress test function
function Invoke-StressTest {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [string]$Token = $null,
        [int]$Count,
        [int]$Parallel
    )

    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $results = @()

    for ($batch = 0; $batch -lt $Count; $batch += $Parallel) {
        $batchSize = [Math]::Min($Parallel, $Count - $batch)
        $jobs = 1..$batchSize | ForEach-Object {
            Start-Job -ScriptBlock {
                param($url, $method, $body, $hdrs)
                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                try {
                    $params = @{
                        Uri = $url
                        Method = $method
                        Headers = $hdrs
                        ContentType = "application/json"
                        ErrorAction = "Stop"
                        TimeoutSec = 30
                    }
                    if ($body) { $params.Body = $body }
                    $response = Invoke-WebRequest @params
                    $sw.Stop()
                    @{ Status = [int]$response.StatusCode; Duration = $sw.Elapsed.TotalMilliseconds; Error = $null }
                } catch {
                    $sw.Stop()
                    $statusCode = 0
                    if ($_.Exception.Response) {
                        $statusCode = [int]$_.Exception.Response.StatusCode
                    }
                    @{ Status = $statusCode; Duration = $sw.Elapsed.TotalMilliseconds; Error = $_.Exception.Message }
                }
            } -ArgumentList $Url, $Method, $Body, $headers
        }
        $jobResults = $jobs | Wait-Job | Receive-Job
        $jobs | Remove-Job -Force
        $results += $jobResults
    }
    return $results
}

function Format-Results($results) {
    $durations = $results | Where-Object { $_.Duration -gt 0 } | ForEach-Object { $_.Duration }
    $statuses = $results | Group-Object -Property Status

    if ($durations.Count -gt 0) {
        $sorted = $durations | Sort-Object
        $avg = ($sorted | Measure-Object -Average).Average
        $min = $sorted[0]
        $max = $sorted[-1]
        $p50 = $sorted[[math]::Floor($sorted.Count * 0.50)]
        $p95 = $sorted[[math]::Floor([math]::Min($sorted.Count * 0.95, $sorted.Count - 1))]
        $p99 = $sorted[[math]::Floor([math]::Min($sorted.Count * 0.99, $sorted.Count - 1))]

        Write-OK ("Avg: {0:N1}ms | P50: {1:N1}ms | P95: {2:N1}ms | P99: {3:N1}ms | Min: {4:N1}ms | Max: {5:N1}ms" -f $avg, $p50, $p95, $p99, $min, $max)
        $statusStr = ($statuses | ForEach-Object { "$($_.Name): $($_.Count)" }) -join ", "
        Write-Info "Status codes: $statusStr"
    }
}

# Step 2: Public endpoints
Write-Header "STEP 2: Stress Testing Public Endpoints ($Iterations reqs x $Concurrency concurrent)"

foreach ($ep in $publicEndpoints) {
    $url = "$BaseUrl$($ep.Path)"
    Write-SubHeader "$($ep.Method) $($ep.Path)"
    $results = Invoke-StressTest -Method $ep.Method -Url $url -Count $Iterations -Parallel $Concurrency
    Format-Results $results
}

# Step 3: Auth endpoints
Write-Header "STEP 3: Stress Testing Auth Endpoints ($Iterations reqs x $Concurrency concurrent)"

foreach ($ep in $authEndpoints) {
    $url = "$BaseUrl$($ep.Path)"
    Write-SubHeader "$($ep.Method) $($ep.Path)"
    $results = Invoke-StressTest -Method $ep.Method -Url $url -Token $token -Count $Iterations -Parallel $Concurrency
    Format-Results $results
}

# Step 4: POST endpoints
Write-Header "STEP 4: Stress Testing POST Endpoints (5 reqs each)"

foreach ($ep in $postEndpoints) {
    $url = "$BaseUrl$($ep.Path)"
    $body = $ep.Body | ConvertTo-Json
    Write-SubHeader "$($ep.Method) $($ep.Path)"
    $results = Invoke-StressTest -Method $ep.Method -Url $url -Body $body -Count 5 -Parallel 2
    Format-Results $results
}

# Step 5: Fetch dashboard metrics
Write-Header "STEP 5: Fetching Metrics Dashboard"
Start-Sleep -Seconds 2

try {
    $metrics = Invoke-RestMethod -Uri "$BaseUrl/metrics/json" -Method GET -ErrorAction Stop

    Write-Host ""
    Write-Host "  +--------+----------------------------------+----------+----------+----------+----------+----------+-------------+" -ForegroundColor DarkCyan
    Write-Host "  | Method | Route                            |  Avg(ms) | P50(ms)  | P95(ms)  | P99(ms)  | Requests | Error Rate  |" -ForegroundColor DarkCyan
    Write-Host "  +--------+----------------------------------+----------+----------+----------+----------+----------+-------------+" -ForegroundColor DarkCyan

    foreach ($ep in $metrics.endpoints) {
        $route = $ep.route
        if ($route.Length -gt 32) { $route = $route.Substring(0, 29) + "..." }

        $line = "  | {0,-6} | {1,-32} | {2,8:N1} | {3,8:N1} | {4,8:N1} | {5,8:N1} | {6,8} | {7,8:N1}%   |" -f $ep.method, $route, $ep.avgDurationMs, $ep.p50DurationMs, $ep.p95DurationMs, $ep.p99DurationMs, $ep.totalRequests, $ep.errorRate

        Write-Host $line -ForegroundColor White
    }

    Write-Host "  +--------+----------------------------------+----------+----------+----------+----------+----------+-------------+" -ForegroundColor DarkCyan

    Write-Host ""
    Write-Host "  Summary: $($metrics.totalRequests) total requests | $($metrics.totalErrors) errors | $($metrics.overallErrorRate)% error rate | $($metrics.activeRequests) active" -ForegroundColor White

    # Status code distribution
    Write-Host ""
    Write-Header "Status Code Distribution"
    foreach ($ep in $metrics.endpoints) {
        $codes = @()
        foreach ($prop in $ep.statusCodeCounts.PSObject.Properties) {
            $codes += "$($prop.Name):$($prop.Value)"
        }
        if ($codes.Count -gt 0) {
            Write-Info "$($ep.method) $($ep.route)  ->  $($codes -join ', ')"
        }
    }

} catch {
    Write-Err "Failed to fetch metrics: $($_.Exception.Message)"
}

Write-Host ""
Write-Header "STRESS TEST COMPLETE"
Write-Host ""
Write-Info "Dashboard: $BaseUrl/metrics/dashboard"
Write-Info "JSON API:  $BaseUrl/metrics/json"
Write-Info "Prometheus: $BaseUrl/metrics/prometheus"
Write-Host ""
