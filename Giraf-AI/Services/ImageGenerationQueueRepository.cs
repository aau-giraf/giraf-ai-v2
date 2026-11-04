using Giraf_AI.DTOs;
using Giraf_AI.Data;
using Giraf_AI.Models;

namespace Giraf_AI.Services;

public sealed class ImageGenerationQueueRepository(GirafAiDbContext dbContext)
    : IImageGenerationQueueRepository
{
    private static readonly IReadOnlyDictionary<string, string> DatabaseIdTypes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CitizenID"] = "citizen_id",
            ["ClassID"] = "class_id",
            ["OrganizationID"] = "organization_id"
        };

    public async Task<int> EnqueueAsync(
        ImageRequest request,
        CancellationToken cancellationToken)
    {
        var queueEntry = new ImageGenerationQueueEntry
        {
            Prompt = request.Prompt,
            ImageId = request.ImgID,
            MapId = request.MapID,
            DatabaseIdType = DatabaseIdTypes[request.IdType]
        };

        dbContext.ImageGenerationQueue.Add(queueEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return queueEntry.QueueId;
    }
}
