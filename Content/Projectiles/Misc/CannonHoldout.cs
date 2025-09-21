using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Terraria.Player;

namespace TheExtraordinaryAdditions.Content.Projectiles.Misc;

public class CannonHoldout : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MatterDisintegrationCannon);

    public Player Owner => Main.player[Projectile.owner];

    public ref float Timer => ref Projectile.ai[0];
    public ref float MoveInIntervals => ref Projectile.ai[1];
    public ref float SpeenBeams => ref Projectile.ai[2];

    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

    public float DrillLength = 90f;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.hide = true;
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = DamageClass.Melee;
    }

    public override bool ShouldUpdatePosition() => false;
    public LoopedSoundInstance LaserSoundSlot;
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);
    public override void AI()
    {
        Timer += 1f;
        SpeenBeams += Timer > 140f ? Owner.Additions().MouseRight.Current ? 2f : 1f : 1f + 2f * Animators.MakePoly(2f).InFunction(1f - Timer / 140f);

        LaserSoundSlot ??= LoopedSoundManager.CreateNew(new(AdditionsSound.LaserHum, () => 1.2f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        LaserSoundSlot.Update(Projectile.Center);

        if (MoveInIntervals > 0f)
            MoveInIntervals -= 1f;

        if (!Owner.channel || !Owner.Available())
        {
            Projectile.Kill();
        }

        else if (MoveInIntervals <= 0f && this.RunLocal())
        {
            Vector2 newVelocity = Owner.Additions().mouseWorld - Center;

            Tile target = Main.tile[tileTargetX, tileTargetY];
            if (target.HasTile)
            {
                newVelocity = new Vector2(tileTargetX, tileTargetY) * 16f + Vector2.One * 8f - Center;
                MoveInIntervals = 2f;
            }

            newVelocity = Vector2.SmoothStep(newVelocity, Projectile.velocity, 0.7f);

            if (float.IsNaN(newVelocity.X) || float.IsNaN(newVelocity.Y))
                newVelocity = -Vector2.UnitY;

            if (newVelocity.Length() < DrillLength)
                newVelocity = newVelocity.SafeNormalize(-Vector2.UnitY) * DrillLength;

            int tileBoost = Owner.inventory[Owner.selectedItem].tileBoost;
            int fullRangeX = (tileRangeX + tileBoost - 1) * 46 + 49;
            int fullRangeY = (tileRangeY + tileBoost - 1) * 46 + 49;
            newVelocity.X = Math.Clamp(newVelocity.X, -fullRangeX, fullRangeX);
            newVelocity.Y = Math.Clamp(newVelocity.Y, -fullRangeY, fullRangeY);
            if (newVelocity != Projectile.velocity)
                Projectile.netUpdate = true;
            Projectile.velocity = newVelocity;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation + (MathHelper.PiOver4 * Owner.gravDir * Owner.direction));
        Projectile.Center = Center + Projectile.velocity; // For the laser

        if (Owner.Additions().SafeMouseRight.Current)
        {
            if (!PlayedSound)
            {
                AdditionsSound.LaserShift.Play(Owner.Center, 2f, 0f, .1f);
                PlayedSound = true;
            }

            DestroyTiles();
        }
        else
            PlayedSound = false;
    }

    private void DestroyTiles()
    {
        if (!this.RunLocal())
            return;

        Vector2 destroyVector = Projectile.Center;
        float destruction = 16f;
        int scale = 3;
        int destructleft = (int)(destroyVector.X / destruction - scale);
        int destructright = (int)(destroyVector.X / destruction + scale);
        int destructdown = (int)(destroyVector.Y / destruction - scale);
        int destructup = (int)(destroyVector.Y / destruction + scale);

        if (destructleft < 0)
            destructleft = 0;
        if (destructright > Main.maxTilesX)
            destructright = Main.maxTilesX;
        if (destructdown < 0)
            destructdown = 0;
        if (destructup > Main.maxTilesY)
            destructup = Main.maxTilesY;

        AchievementsHelper.CurrentlyMining = true;
        for (int x = destructleft; x <= destructright; x++)
        {
            for (int y = destructdown; y <= destructup; y++)
            {
                float circX = Math.Abs(x - destroyVector.X / destruction);
                float circY = Math.Abs(y - destroyVector.Y / destruction);
                if (!(Math.Sqrt(circX * circX + circY * circY) < scale) || !(Main.tile[x, y] != null))
                    continue;

                Tile val = Main.tile[x, y];
                if (val.HasTile)
                {
                    WorldGen.KillTile(x, y, false, false, false);

                    Vector2 pos = new Point(x, y).ToWorldCoordinates();
                    for (int i = 0; i < 8; i++)
                        ParticleRegistry.SpawnSparkParticle(pos, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(20, 30),
                            Main.rand.NextFloat(.3f, .7f), Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(0f, .3f)), true, true);

                    val = Main.tile[x, y];
                    if (!val.HasTile && Main.netMode != NetmodeID.SinglePlayer)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y, 0f, 0, 0, 0);
                }
            }
        }
        AchievementsHelper.CurrentlyMining = false;
    }

    public const float around = 2f;
    public const float amount = 3f;
    public const float scale = 6.1f;
    public const int amountbeams = 3;
    public void DrawBeam(Texture2D beamTex, Vector2 direction, int beamIndex, bool second)
    {
        Vector2 val = Center + direction * (Owner.Additions().MouseRight.Current ? 20f : 10f);
        Vector2 val2 = default;
        Vector2 startPos = val + direction.RotatedBy(MathHelper.PiOver2, val2) * MathF.Cos(MathHelper.Pi * around * beamIndex / amount + SpeenBeams * 0.06f) * (Owner.Additions().MouseRight.Current ? 24f : 12f);
        float rotation = (Projectile.Center - startPos).ToRotation();
        Vector2 beamOrigin = new(beamTex.Width / 2f, beamTex.Height);
        val2 = startPos - Projectile.Center;
        Vector2 beamScale = new(scale, ((Vector2)val2).Length() / beamTex.Height);
        val2 = default;

        if (second)
        {
            val = Center + direction * (Owner.Additions().MouseRight.Current ? 40f : 20f);
            val2 = default;
            startPos = val + direction.RotatedBy(MathHelper.PiOver2, val2) * MathF.Cos(MathHelper.Pi * around * beamIndex / amount + SpeenBeams * 0.06f) * (Owner.Additions().MouseRight.Current ? 48f : 24f);
            rotation = (Projectile.Center - startPos).ToRotation();
            beamOrigin = new(beamTex.Width / 2f, beamTex.Height);
            val2 = startPos - Projectile.Center;
            beamScale = new(scale, ((Vector2)val2).Length() / beamTex.Height);
            val2 = default;
        }

        DrawChromaticAberration(direction.RotatedBy(MathHelper.PiOver2, val2), 4f, delegate (Vector2 offset, Color colorMod)
        {
            Color val3 = Color.Lerp(Color.Red, Color.Goldenrod, Sin01(SpeenBeams * 0.2f));
            val3 *= 0.54f;
            val3 = val3.MultiplyRGB(colorMod);
            Main.EntitySpriteDraw(beamTex, startPos + offset - Main.screenPosition, null, val3, rotation + MathHelper.PiOver2, beamOrigin, beamScale, 0, 0f);
            beamScale.X = 2.4f;
            val3 = Color.Lerp(Color.OrangeRed, Color.Goldenrod, Sin01(SpeenBeams * 0.2f + 1.2f));
            val3 = val3.MultiplyRGB(colorMod);
            Main.EntitySpriteDraw(beamTex, startPos + offset - Main.screenPosition, null, val3, rotation + MathHelper.PiOver2, beamOrigin, beamScale, 0, 0f);
        });
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
        Vector2 origin = new Vector2(9f, tex.Height / 2f);
        SpriteEffects effect = 0;
        if (Owner.direction * Owner.gravDir < 0f)
            effect = SpriteEffects.FlipVertically;

        Vector2 normalizedVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Vector2 pos = Center + normalizedVelocity * 10f - Main.screenPosition;

        Main.EntitySpriteDraw(tex, pos, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effect, 0f);

        Main.spriteBatch.SetBlendState(BlendState.Additive);

        Texture2D beamTex = AssetRegistry.GetTexture(AdditionsTexture.SimpleGradient);

        for (int j = 0; j < amountbeams; j++)
        {
            if (MathF.Sin(MathHelper.Pi * around * j / amount + SpeenBeams * 0.06f) < 0f)
            {
                DrawBeam(beamTex, -normalizedVelocity, j, false);
                DrawBeam(beamTex, normalizedVelocity, j, true);
            }
        }

        Texture2D bloomTex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        Main.EntitySpriteDraw(bloomTex, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * 0.3f, MathHelper.PiOver2, bloomTex.Size() / 2f, 0.3f * Projectile.scale, 0, 0f);

        ManagedShader shader = ShaderRegistry.FadedStreak;
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DoubleTrail), 1);

        Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.MatterDisintegrationCannonBloom);
        float bloomOpacity = Animators.MakePoly(3f).InFunction(InverseLerp(0f, 30f, Timer)) * (0.85f + Sin01(Main.GlobalTimeWrappedHourly)) * 0.8f;
        Color bloomColor = Color.Lerp(Color.OrangeRed, Color.Chocolate, Sin01(SpeenBeams * 0.2f + 1.2f));
        for (int i = 0; i < 8; i++)
            Main.EntitySpriteDraw(bloom, pos + (MathHelper.TwoPi * i / 8).ToRotationVector2() * 2f, null, bloomColor * bloomOpacity, Projectile.rotation, origin, Projectile.scale, effect, 0f);

        for (int i = 0; i < amountbeams; i++)
        {
            if (MathF.Sin(MathHelper.Pi * around * i / amount + SpeenBeams * 0.06f) >= 0f)
            {
                DrawBeam(beamTex, -normalizedVelocity, i, false);
                DrawBeam(beamTex, normalizedVelocity, i, true);
            }
        }

        Main.spriteBatch.ResetToDefault();
        return false;
    }
}