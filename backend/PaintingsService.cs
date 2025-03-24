using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace WebXrPaintings;

public class PaintingsService(IOptions<Config> config, IContentTypeProvider contentTypeProvider)
{
    private readonly Config config = config.Value;

    public string CreatePaintingPath(string id, string originalFileName) =>
        Path.Join(
            config.PaintingsPath,
            Path.ChangeExtension(id, Path.GetExtension(originalFileName))
        );

    public string CreateConfigPath(string id) =>
        Path.Join(config.PaintingsPath, Path.ChangeExtension(id, "json"));

    public bool TryGetPainting(
        string id,
        [NotNullWhen(true)] out string? path,
        [NotNullWhen(true)] out string? contentType
    )
    {
        var searcher = new PaintingImageSearcher(contentTypeProvider, id);
        SearchPaintingFiles(searcher);
        if (searcher.Painting is (var foundPath, var foundContentType))
        {
            path = foundPath;
            contentType = foundContentType;
            return true;
        }

        path = null;
        contentType = null;
        return false;
    }

    public bool TryGetConfig(string id, out string? path)
    {
        var imageSearcher = new PaintingImageSearcher(contentTypeProvider, id);
        var configSearcher = new PaintingConfigSearcher(contentTypeProvider, id);
        SearchPaintingFiles(imageSearcher, configSearcher);
        if (imageSearcher.Painting is (_, _))
        {
            path = configSearcher.ConfigPath;
            return true;
        }

        path = null;
        return false;
    }

    private void SearchPaintingFiles(params IPaintingSearcher[] visitors)
    {
        foreach (var path in Directory.EnumerateFiles(config.PaintingsPath))
        {
            foreach (var visitor in visitors)
            {
                visitor.Check(path);
            }
        }
    }

    private interface IPaintingSearcher
    {
        void Check(string path);
    }

    private class PaintingImageSearcher(IContentTypeProvider contentTypeProvider, string id)
        : IPaintingSearcher
    {
        public Tuple<string, string>? Painting => state.State;

        private SearchState<Tuple<string, string>> state = new SearchState<
            Tuple<string, string>
        >.Empty();

        public void Check(string path)
        {
            if (
                Path.GetFileNameWithoutExtension(path) == id
                && contentTypeProvider.TryGetContentType(path, out var contentType)
                && contentType.StartsWith("image/")
            )
            {
                state = state.Update(new(Path.GetFullPath(path), contentType));
            }
        }
    }

    private class PaintingConfigSearcher(IContentTypeProvider contentTypeProvider, string id)
        : IPaintingSearcher
    {
        public string? ConfigPath => state.State;

        private SearchState<string> state = new SearchState<string>.Empty();

        public void Check(string path)
        {
            if (
                Path.GetFileNameWithoutExtension(path) == id
                && contentTypeProvider.TryGetContentType(path, out var contentType)
                && contentType == MediaTypeNames.Application.Json
            )
            {
                state = state.Update(Path.GetFullPath(path));
            }
        }
    }

    private abstract class SearchState<TState>
        where TState : class
    {
        public TState? State { get; private protected init; } = null;

        public abstract SearchState<TState> Update(TState state);

        public class Empty : SearchState<TState>
        {
            public override SearchState<TState> Update(TState state) => new Found(state);
        }

        public class Found : SearchState<TState>
        {
            public Found(TState state) => State = state;

            public override SearchState<TState> Update(TState state) => new Duplicate();
        }

        public class Duplicate : SearchState<TState>
        {
            public override SearchState<TState> Update(TState state) => this;
        }
    }
}
