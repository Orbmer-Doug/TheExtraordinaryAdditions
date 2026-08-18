using ReLogic.Content;
using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Assets;

public readonly struct LazyAsset<T>(Func<Asset<T>> assetLoadFunction, string path) where T : class
{
    private readonly Lazy<Asset<T>> asset = new(assetLoadFunction);

    public Asset<T> Asset => asset.Value;

    public bool Uninitialized => asset is null;

    public T Value => asset.Value.Value;

    public readonly string Path = path;

    public static LazyAsset<T> FromPath(string path, AssetRequestMode requestMode = AssetRequestMode.AsyncLoad)
    {
        return new LazyAsset<T>(() => ModContent.Request<T>(path, requestMode), path);
    }

    public static implicit operator T(LazyAsset<T> asset) => asset.Value;
}

public static class LazyExt
{
    extension(LazyAsset<Texture2D> tex)
    {
        public int Height() => tex.Asset.Height();
        public int Width() => tex.Asset.Width();
        public Vector2 Size() => tex.Asset.Size();
    }
}
