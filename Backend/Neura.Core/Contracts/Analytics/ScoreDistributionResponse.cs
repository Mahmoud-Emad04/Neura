namespace Neura.Core.Contracts.Analytics;

public class ScoreDistributionResponse
{
    public List<ScoreBucket> Buckets { get; set; } = new();
}

public class ScoreBucket
{
    public string Range { get; set; } = string.Empty;  // e.g., "0-10", "11-20", ..., "91-100"
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}