using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Early.BiomePointer;

namespace TheExtraordinaryAdditions.UI;

public class BiomePointerUI : SmartUIState
{
    public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
    public override InterfaceScaleType Scale => InterfaceScaleType.None;

    internal static float NoTileCompletion = 0f;
    internal static float TileCompletion = 0f;
    internal static float Scroll = 0f;
    internal static float ClickCooldown = 0f;
    internal static bool CurrentlyViewing;
    public override bool Visible => CurrentlyViewing;
    public override void Draw(SpriteBatch spriteBatch)
    {
        Player player = Main.LocalPlayer;

        if (player == null || player.active == false || player.heldProj == -1)
            return;

        Projectile proj = Main.projectile[player.heldProj] ?? null;

        if (proj == null || proj.active == false || proj.type != ModContent.ProjectileType<BiomePointer>())
            return;

        BiomePointer pointer = proj.As<BiomePointer>();

        Vector2 backgroundScale = Vector2.One * Main.UIScale;
        Vector2 center = player.Center + (Vector2.UnitY * -60f * backgroundScale.Y) - Main.screenPosition;
        Vector2 position = center + new Vector2(0f, -42f * backgroundScale.Y);

        spriteBatch.Draw(back, position, null, Color.White, 0f, back.Size() / 2, backgroundScale, 0, 0f);
        spriteBatch.Draw(arrow, center, null, Color.White, proj.rotation, arrow.Size() / 2, backgroundScale, 0, 0f);

        if (NoTileCompletion > 0f)
        {
            position += Main.rand.NextVector2CircularEdge(NoTileCompletion * 3f, NoTileCompletion * 3f);
            NoTileCompletion -= .05f;
        }

        if (TileCompletion > 0f)
            TileCompletion -= .05f;

        if (Scroll > 0f)
            Scroll -= .05f;

        float interpolant = 1f - InverseLerp(0f, 1f, Scroll);
        Vector2 finalScale = backgroundScale * interpolant;
        Color drawCol = proj.GetAlpha(Color.White.Lerp(Color.Red, NoTileCompletion).Lerp(Color.Green, TileCompletion)) * interpolant;

        switch (pointer.Mode)
        {
            case BlockToPointTo.Marble:
                Main.EntitySpriteDraw(marble, position, null, drawCol, 0f, marble.Size() / 2, finalScale, 0, 0);
                break;
            case BlockToPointTo.Granite:
                Main.EntitySpriteDraw(granite, position, null, drawCol, 0f, granite.Size() / 2, finalScale, 0, 0);
                break;
            case BlockToPointTo.Mushroom:
                Main.EntitySpriteDraw(mushroom, position, null, drawCol, 0f, mushroom.Size() / 2, finalScale, 0, 0);
                break;
            case BlockToPointTo.Hive:
                Main.EntitySpriteDraw(hive, position, null, drawCol, 0f, hive.Size() / 2, finalScale, 0, 0);
                break;
            case BlockToPointTo.Shimmer:
                Main.EntitySpriteDraw(shimmer, position, null, drawCol, 0f, shimmer.Size() / 2, finalScale, 0, 0);
                break;
        }
    }

    public static readonly Texture2D arrow = AssetRegistry.GetTexture(AdditionsTexture.BiomePointer);
    public static readonly Texture2D back = AssetRegistry.GetTexture(AdditionsTexture.BiomePointerBackground);
    public static readonly Texture2D marble = ModContent.Request<Texture2D>(ItemID.Marble.GetTerrariaItem(), AssetRequestMode.ImmediateLoad).Value;
    public static readonly Texture2D granite = ModContent.Request<Texture2D>(ItemID.Granite.GetTerrariaItem(), AssetRequestMode.ImmediateLoad).Value;
    public static readonly Texture2D mushroom = ModContent.Request<Texture2D>(ItemID.MushroomGrassSeeds.GetTerrariaItem(), AssetRequestMode.ImmediateLoad).Value;
    public static readonly Texture2D hive = ModContent.Request<Texture2D>(ItemID.Hive.GetTerrariaItem(), AssetRequestMode.ImmediateLoad).Value;
    public static readonly Texture2D shimmer = ModContent.Request<Texture2D>(ItemID.ShimmerBlock.GetTerrariaItem(), AssetRequestMode.ImmediateLoad).Value;
}