using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Summon;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Misc;

public class BeetleHoldout : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimsonCarvedBeetle);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 5;
    }

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 66;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public ref float Time => ref Projectile.ai[0];
    public bool Right
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public int AnimTime
    {
        get
        {
            return Right ? 50 : 90;
        }
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public float RiseInterpolant => InverseLerp(0f, AnimTime, Time);
    public float Interpolant => GetLerpBump(0f, AnimTime - 10f, AnimTime, AnimTime - 10f, Time);
    public override void AI()
    {
        if (this.RunLocal() && (!Owner.channel && !Right || !Modded.MouseRight.Current && Right))
        {
            Projectile.Kill();
            return;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.SetAnimation(5, 15);

        Vector2 rise = new(Owner.direction * Projectile.width * .6f, -30f * Animators.MakePoly(3f).InFunction(RiseInterpolant) * Owner.gravDir);
        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        Projectile.Center = center + rise;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, center.SafeDirectionTo(Projectile.Center).ToRotation() * Owner.gravDir);
        Projectile.Opacity = Interpolant;

        if (Main.rand.NextBool((int)(10 * (1.2f - RiseInterpolant))))
        {
            ParticleRegistry.SpawnCloudParticle(Projectile.RotHitbox().RandomPoint(), Main.rand.NextVector2Circular(2f, 2f), Color.Crimson, Color.DarkRed, Main.rand.Next(20, 40), Main.rand.NextFloat(.3f, .45f), .2f + RiseInterpolant * .8f);
        }
        if (Main.rand.NextBool((int)(5 * (1.2f - RiseInterpolant))))
        {
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(), Vector2.UnitY * -(Main.rand.NextFloat(2f, 5f) * Interpolant), Main.rand.Next(30, 45), Main.rand.NextFloat(.4f, .6f), Color.DarkRed, Color.Crimson * 1.4f, null, 1f, 3);
        }

        if (Time >= AnimTime)
        {
            SoundID.Roar.Play(Owner.Center, 1f, -.2f);

            if (Right)
            {
                if (this.RunLocal())
                {
                    if (!SuperBloodMoonSystem.SuperBloodMoon)
                    {
                        SuperBloodMoonSystem.SuperBloodMoon = true;
                        DisplayText(ModContent.GetModItem(ModContent.ItemType<CrimsonCarvedBeetle>()).GetLocalizedValue("TurnOn"), Color.Crimson);
                    }
                    else
                    {
                        SuperBloodMoonSystem.SuperBloodMoon = false;
                        DisplayText(ModContent.GetModItem(ModContent.ItemType<CrimsonCarvedBeetle>()).GetLocalizedValue("TurnOff"), Color.DarkRed);
                    }

                    AdditionsNetcode.SyncAdditionsBloodMoon(Main.myPlayer);
                }
                
                AdditionsNetcode.SyncWorld();
            }
            else
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(Owner.whoAmI, ModContent.NPCType<StygainHeart>());
                else
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, Owner.whoAmI, ModContent.NPCType<StygainHeart>(), 0f, 0f, 0, 0, 0);
            }

            Projectile.netUpdate = true;
            Projectile.Kill();
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 pos = Projectile.Center;
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Color col = Projectile.GetAlpha(lightColor);
        Vector2 orig = frame.Size() / 2;
        SpriteEffects fx = Owner.gravDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        if (Owner.direction == 1)
            fx |= SpriteEffects.FlipHorizontally;

        Main.spriteBatch.DrawBetter(tex, pos, frame, col, 0f, orig, 1f, fx);

        return false;
    }
}