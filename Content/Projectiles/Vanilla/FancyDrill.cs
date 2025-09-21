using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla;

public class FancyDrill : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Drill => Owner.HeldItem;
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height);
        return new(Projectile.Center - size / 2, size, Projectile.rotation);
    }

    public override void AI()
    {
        if ((!Owner.Available() || !Owner.channel) && this.RunLocal())
        {
            Projectile.Kill();
            return;
        }

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Modded.mouseWorld), .7f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = center + PolarVector(Projectile.width / 2, Projectile.rotation);
        Projectile.width = Drill.ThisItemTexture().Width;
        Projectile.height = Drill.ThisItemTexture().Height;
        Projectile.damage = Drill.damage;
        Projectile.knockBack = Drill.knockBack;
        Projectile.spriteDirection = Projectile.direction;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Projectile.soundDelay <= 0 && Time > 10)
        {
            SoundID.Item22.Play(Projectile.Center, 1f, 0f, .05f);
            SolidCollision(Rect(), 4f);
            Projectile.soundDelay = (int)Clamp(35 - Drill.useTime, 4, 100);
        }
        Projectile.Opacity = InverseLerp(0f, 10f, Time);

        // Gives the drill a slight jiggle
        Projectile.position += -Projectile.velocity * Main.rand.NextFloat(.5f, 1.5f);

        // Spawning dust
        if (Main.rand.NextBool(10) && Projectile.Opacity >= 1f)
        {
            ParticleRegistry.SpawnMistParticle(Rect().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.5f, 2f), Main.rand.NextFloat(.1f, .3f), Color.Gray, Color.DarkGray, Main.rand.NextFloat(100f, 255f), Main.rand.NextFloat(-.2f, .3f));
        }
        Time++;
    }

    public void SolidCollision(RotatedRectangle rect, float sampleIncrement = 1f, bool acceptTopSurfaces = false)
    {
        // Calculate the corners of the rotated rectangle
        Vector2 topLeft = rect.Position;
        Vector2 topRight = rect.TopRight;
        Vector2 bottomLeft = rect.BottomLeft;
        Vector2 bottomRight = rect.BottomRight;

        // Determine the number of samples to take along the width and height
        int widthSamples = (int)(rect.Width / sampleIncrement);
        int heightSamples = (int)(rect.Height / sampleIncrement);

        // Sample points in the area
        for (int i = 0; i <= widthSamples; i++)
        {
            // Lerp between the sides
            float interpolant = (float)i / widthSamples;
            Vector2 left = Vector2.Lerp(topLeft, bottomLeft, interpolant);
            Vector2 right = Vector2.Lerp(topRight, bottomRight, interpolant);

            for (int j = 0; j <= heightSamples; j++)
            {
                // Lerp inbetween the side interpolants
                Vector2 samplePoint = Vector2.Lerp(left, right, (float)j / heightSamples);

                // Convert to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                if (tile != null && tile.HasTile && !Main.tileAxe[tile.TileType])
                {
                    ToolModifierUtils.Mine(Owner, Drill, false, false, tilePoint);

                    if (Drill.pick <= 0 && Drill.axe <= 0 && Drill.hammer <= 0)
                        return;

                    bool flag = Owner.IsTargetTileInItemRange(Drill);
                    if (Owner.noBuilding)
                        flag = false;

                    if (!flag)
                        return;

                    for (int f = 0; f < 12; f++)
                        ParticleRegistry.SpawnSparkParticle(tilePoint.ToWorldCoordinates(), Main.rand.NextVector2Circular(9f, 9f),
                            Main.rand.Next(12, 22), Main.rand.NextFloat(.4f, .6f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat()), true, true);
                }
            }
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Drill.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor;
        float scale = Projectile.scale;
        float rotation = Projectile.rotation;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += Pi;
        }

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Drill.Name.Contains("Solar"))
            col = Color.White;
        Main.spriteBatch.Draw(tex, pos, frame, col * Projectile.Opacity, rotation, orig, scale, direction, 0f);

        if (Drill.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Drill.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, Color.White * Projectile.Opacity, rotation, orig, scale, direction, 0f);
        }

        return false;
    }
}
