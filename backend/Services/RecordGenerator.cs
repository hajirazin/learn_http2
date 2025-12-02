using System.Runtime.CompilerServices;
using Http2Streaming.Api.Models;

namespace Http2Streaming.Api.Services;

public static class RecordGenerator
{
    public static async IAsyncEnumerable<RecordDto> GenerateRecords(
        int totalRecords = 100_000,
        int chunkSize = 1_000,
        int delayMilliseconds = 500,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();

        for (var i = 1; i <= totalRecords; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = new RecordDto(
                Id: i,
                Name: $"Item {i}",
                Value: Math.Round((decimal)(random.NextDouble() * 1000), 2),
                CreatedAt: DateTime.UtcNow);

            yield return record;

            if (i % chunkSize == 0)
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
    }
}


