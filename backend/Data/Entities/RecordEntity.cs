namespace Http2Streaming.Api.Data.Entities;

public class RecordEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedAt { get; set; }
}

