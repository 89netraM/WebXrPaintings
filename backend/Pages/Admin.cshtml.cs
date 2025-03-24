using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebXrPaintings.Pages;

public partial class Admin(PaintingsService paintingsService) : PageModel
{
    public bool Exists { get; private set; } = false;
    public string? Config { get; private set; } = null;

    public async Task OnGetAsync([FromRoute] string id)
    {
        Exists = paintingsService.TryGetConfig(id, out var configPath);
        if (configPath is not null)
        {
            Config = await System.IO.File.ReadAllTextAsync(configPath);
        }
    }

    public async Task<IActionResult> OnPostAsync(
        [FromRoute] string id,
        [FromForm] IFormFile? replacement,
        [FromForm] string? config
    )
    {
        if (!paintingsService.TryGetPainting(id, out _, out _) && replacement is null)
        {
            return BadRequest();
        }

        Exists = true;

        if (replacement is not null)
        {
            using var replacementStream = System.IO.File.OpenWrite(
                paintingsService.CreatePaintingPath(id, replacement.FileName)
            );
            await replacement.OpenReadStream().CopyToAsync(replacementStream);
        }

        var configPath = paintingsService.CreateConfigPath(id);
        if (config is not null)
        {
            using var configWriter = new StreamWriter(configPath);
            await configWriter.WriteAsync(config);
            Config = config;
        }
        else if (Path.Exists(configPath))
        {
            Config = await System.IO.File.ReadAllTextAsync(configPath);
        }

        return Page();
    }
}
