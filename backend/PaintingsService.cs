using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace WebXrPaintings;

public class PaintingsService(IOptions<Config> config, IContentTypeProvider contentTypeProvider)
{
    private readonly Config config = config.Value;

    public bool TryGetPainting(
        string id,
        [NotNullWhen(true)] out string? path,
        [NotNullWhen(true)] out string? contentType
    )
    {
        if (
            Directory.GetFiles(config.PaintingsPath, $"{id}.*") is [var painting]
            && contentTypeProvider.TryGetContentType(painting, out contentType)
            && contentType.StartsWith("image/")
        )
        {
            path = Path.GetFullPath(painting);
            return true;
        }

        path = null;
        contentType = null;
        return false;
    }
}
