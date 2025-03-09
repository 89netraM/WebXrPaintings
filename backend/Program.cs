using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QRCoder;
using WebXrPaintings;

var builder = WebApplication.CreateSlimBuilder(args);

var config = builder.ConfigureConfig();

builder.Services.AddSingleton<QRCodeGenerator>();

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

app.Run();
