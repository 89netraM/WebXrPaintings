using System;
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QRCoder;
using WebXrPaintings;

var builder = WebApplication.CreateSlimBuilder(args);

var config = builder.ConfigureConfig();

builder.Services.AddSingleton<QRCodeGenerator>();
builder.Services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();
builder.Services.AddSingleton<PaintingsService>();

var app = builder.Build();

app.UsePathBase(config.BaseUrl.AbsolutePath);

app.UseStaticFiles();

app.MapGet(
    "/{id}/target",
    static (
        [FromServices] IOptions<Config> config,
        [FromServices] QRCodeGenerator qrCodeGenerator,
        [FromServices] PaintingsService paintingsService,
        [FromRoute] string id
    ) =>
    {
        if (!paintingsService.TryGetPainting(id, out _, out _))
        {
            return Results.NotFound();
        }

        var url = new Uri(config.Value.BaseUrl, id);
        var qrCode = qrCodeGenerator.CreateQrCode(url.ToString(), QRCodeGenerator.ECCLevel.H);
        var png = new PngByteQRCode(qrCode);
        return Results.Bytes(png.GetGraphic(32), MediaTypeNames.Image.Png);
    }
);

app.MapGet(
    "/{id}/replacement",
    static ([FromServices] PaintingsService paintingsService, [FromRoute] string id) =>
        paintingsService.TryGetPainting(id, out var painting, out var contentType)
            ? Results.File(painting, contentType)
            : Results.NotFound()
);

app.UseRewriter(new RewriteOptions().AddRedirect("(.*[^/])$", "$1/"));

app.MapGet(
    "/{id}",
    static ([FromServices] PaintingsService paintingsService, [FromRoute] string id) =>
        paintingsService.TryGetPainting(id, out _, out _)
            ? Results.File(Path.GetFullPath("wwwroot/index.html"), MediaTypeNames.Text.Html)
            : Results.NotFound()
);

app.Run();
