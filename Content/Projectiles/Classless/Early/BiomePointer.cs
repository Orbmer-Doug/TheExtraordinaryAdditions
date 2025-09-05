using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;

public class BiomePointer : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BiomePointer);
    public override int AssociatedItemID => ModContent.ItemType<BiomeFinder>();
    public override int IntendedProjectileType => ModContent.ProjectileType<BiomePointer>();

    public override void Defaults()
    {
        Projectile.width = 32;
        Projectile.height = 30;
    }

    public enum BlockToPointTo
    {
        Marble = TileID.Marble,
        Granite = TileID.Granite,
        Mushroom = TileID.MushroomGrass,
        Hive = TileID.Hive,
        Shimmer = TileID.ShimmerBlock,
    }
    public BlockToPointTo Mode
    {
        get => (BlockToPointTo)Projectile.ai[0];
        set => Projectile.ai[0] = (ushort)value;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Mode = BlockToPointTo.Marble;
        Projectile.netUpdate = true;
    }

    public List<Vector2> Coords = [];
    public static readonly int MaxTiles = 2000;
    public static List<(Vector2 coord, BlockToPointTo mode, Vector2 playerPos)> cachedCoords = [];
    public const float MaxPlayerDist = 1000f;
    public static readonly float maxRadius = MaxTiles * 16f;
    public override void SafeAI()
    {
        BiomePointerUI.CurrentlyViewing = true;
        Owner.heldProj = Projectile.whoAmI;
        Projectile.timeLeft = 2;
        Projectile.Center = Vector2.Lerp(Projectile.Center, Owner.Center + new Vector2(Owner.direction * 20f, 0f), .8f);
        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.ToRotation(), .18f);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Owner.direction == 1 ? -MathHelper.PiOver4 : MathHelper.PiOver4);

        if (this.RunLocal() && Modded.SafeMouseLeft.JustPressed)
        {
            Coords.Clear();

            // Check if cache is valid (same mode and player hasn't moved too far)
            if (cachedCoords.Count > 0 && cachedCoords[0].mode == Mode &&
                Vector2.Distance(Owner.Center, cachedCoords[0].playerPos) < MaxPlayerDist)
            {
                Coords.AddRange(cachedCoords.Select(c => c.coord));
            }
            else
            {
                const int maxChecks = 5000;
                int checks = 0;
                const float stepSize = 16f;
                const float angleStep = 0.5f;
                float currentRadius = 0f;

                // Spiral search, more efficient than brute force checking but isn't perfect
                while (currentRadius <= maxRadius && checks < maxChecks)
                {
                    for (float angle = 0; angle < MathHelper.TwoPi; angle += angleStep)
                    {
                        Vector2 offset = (angle + RandomRotation() * 0.1f).ToRotationVector2() * currentRadius;
                        Vector2 worldCoord = Owner.Center + offset;
                        Point tilePos = worldCoord.ToTileCoordinates();

                        if (!WorldGen.InWorld(tilePos.X, tilePos.Y))
                            continue;

                        Tile tile = Main.tile[tilePos.X, tilePos.Y];
                        checks++;

                        // Check for valid tile or liquid
                        if (Mode == BlockToPointTo.Shimmer)
                        {
                            if (tile.Get<LiquidData>().LiquidType == LiquidID.Shimmer && tile.Get<LiquidData>().Amount > 0)
                            {
                                Coords.Add(worldCoord);
                            }
                        }
                        else if (tile.HasTile && tile.TileType == (ushort)Mode)
                        {
                            Coords.Add(worldCoord);
                        }

                        // Stop early if we have enough coords
                        if (Coords.Count >= 15)
                            goto FoundEnough;
                    }
                    currentRadius += stepSize;
                }

            FoundEnough:
                // Cache the results
                cachedCoords.Clear();
                cachedCoords.AddRange(Coords.Select(c => (c, Mode, Owner.Center)));
            }

            if (Coords.Count == 0)
            {
                BiomePointerUI.NoTileCompletion = .8f;
                SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = -.5f }, Owner.Center);
                return;
            }

            BiomePointerUI.TileCompletion = .8f;
            SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = .5f }, Owner.Center);

            // Find closest tile
            Projectile.velocity = Projectile.SafeDirectionTo(Coords.OrderBy(x => x.Distance(Owner.Center)).First());

            float offsetAngle = RandomRotation();
            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / count + offsetAngle).ToRotationVector2() * 5f;
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, shootVelocity, 30, 32f, Color.Green);
            }
            this.Sync();
        }

        // Allow switching of modes
        if (this.RunLocal() && Modded.SafeMouseRight.JustPressed)
        {
            switch (Mode)
            {
                case BlockToPointTo.Marble:
                    Mode = BlockToPointTo.Granite;
                    break;
                case BlockToPointTo.Granite:
                    Mode = BlockToPointTo.Mushroom;
                    break;
                case BlockToPointTo.Mushroom:
                    Mode = BlockToPointTo.Hive;
                    break;
                case BlockToPointTo.Hive:
                    Mode = BlockToPointTo.Shimmer;
                    break;
                case BlockToPointTo.Shimmer:
                    Mode = BlockToPointTo.Marble;
                    break;
            }
            BiomePointerUI.Scroll = 1f;
            this.Sync();
        }
    }

    public override void OnKill(int timeLeft)
    {
        BiomePointerUI.CurrentlyViewing = false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Owner.HeldItem.ThisItemTexture();
        Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(tex, drawPosition, null, color, 0f, tex.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}