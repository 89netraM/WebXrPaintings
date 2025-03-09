using System;
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QRCoder;
using WebXrPaintings;

var builder = WebApplication.CreateSlimBuilder(args);

var config = builder.ConfigureConfig();

builder.Services.AddSingleton<QRCodeGenerator>();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

var app = builder.Build();

app.UsePathBase(config.BaseUrl.AbsolutePath);

app.MapGet(
    "/{id}/target",
    static (
        [FromServices] IOptions<Config> config,
        [FromServices] QRCodeGenerator qrCodeGenerator,
        [FromRoute] string id
    ) =>
    {
        var url = new Uri(config.Value.BaseUrl, id);
        var qrCode = qrCodeGenerator.CreateQrCode(url.ToString(), QRCodeGenerator.ECCLevel.H);
        var png = new PngByteQRCode(qrCode);
        return Results.Bytes(png.GetGraphic(32), MediaTypeNames.Image.Png);
    }
);

app.MapGet(
    "/{id}/replacement",
    static (
        [FromServices] IOptions<Config> config,
        [FromServices] FileExtensionContentTypeProvider contentTypeProvider,
        [FromRoute] string id
    ) =>
        Directory.GetFiles(config.Value.PaintingsPath, $"{id}.*") is [var painting]
        && contentTypeProvider.TryGetContentType(painting, out var contentType)
        && contentType.StartsWith("image/")
            ? Results.File(Path.GetFullPath(painting), contentType)
            : Results.NotFound()
);

app.Run();
