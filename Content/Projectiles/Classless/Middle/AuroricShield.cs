using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class AuroricShield : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AuroricShield);

    private const int MaxTime = 50;
    public override void SetDefaults()
    {
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = MaxTime;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = 50;
        Projectile.height = 122;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Parried
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Offset);
    public override void ReceiveExtraAI(BinaryReader reader) => Offset = reader.ReadVector2();
    
    public Player Owner => Main.player[Projectile.owner];
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.RotHitbox().Top, Projectile.RotHitbox().Bottom, Projectile.width, ref _);
    }

    public override void AI()
    {
        Vector2 center = Owner.RotatedRelativePoint(Owner.Center, true, true);
        if (Time == 0f)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = center.SafeDirectionTo(Owner.Additions().mouseWorld);
                this.Sync();
            }
        }

        if (Time < 20f)
            Offset = Vector2.Lerp(new(0f, 50f), new(0f, 10f), Circ.InFunction(InverseLerp(0f, 20f, Time)));
        else if ((int)Time == 30)
            SoundID.Item1.Play(Projectile.Center, 1.1f, -.2f, .1f, null);
        else if (Time < 30f)
            Offset = Vector2.Lerp(new(0f, 10f), new(0f, 100f), Exp().OutFunction(InverseLerp(20f, 30f, Time)));
        else if (Time < MaxTime)
            Offset = Vector2.Lerp(new(0f, 100f), new(0f, 10f), MakePoly(3).InFunction(InverseLerp(30f, MaxTime, Time)));

        Projectile.scale = Projectile.Opacity = GetLerpBump(0f, 10f, MaxTime, MaxTime - 10f, Time);
        Projectile.Center = center + Offset.RotatedBy(Projectile.rotation - MathHelper.PiOver2);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir((Projectile.Center.X > Owner.Center.X).ToDirectionInt());
        Owner.heldProj = Projectile.whoAmI;
        Time++;

        if (Time.BetweenNum(20f, 30f))
        {
            float _ = 0f;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p == null || !p.active || p.Size.Length() > 300f || p.whoAmI == Projectile.whoAmI || !p.hostile || p.damage <= 1)
                    continue;
                if (!Collision.CheckAABBvLineCollision(p.Hitbox.TopLeft(), p.Hitbox.Size(), Projectile.RotHitbox().Top, Projectile.RotHitbox().Bottom, Projectile.width, ref _))
                    continue;

                if (!Parried)
                {
                    if (p.velocity != Vector2.Zero)
                        p.velocity += Projectile.velocity * 10f;

                    ParryEffects();
                    ProjectileDamageModifiers dam = p.ProjDamageMod();
                    if (dam.FlatDR < 100)
                        dam.FlatDR = 100;
                    if (dam.FlatDRTimer < 90)
                        dam.FlatDRTimer = 90;

                    Parried = true;
                }
            }
        }
    }

    public override bool? CanDamage()
    {
        return Time.BetweenNum(20f, 30f) ? null : false;
    }

    private void ParryEffects()
    {
        ScreenShakeSystem.New(new(5f, .6f), Projectile.Center);
        AdditionsSound.ColdPunch.Play(Projectile.Center, 1.2f, -.1f, .2f);
        for (int i = 0; i < 18; i++)
        {
            Vector2 pos = Projectile.Hitbox.ToRotated(Projectile.rotation).RandomPoint();
            if (i % 2 == 1)
            {
                ParticleRegistry.SpawnCloudParticle(pos, Projectile.velocity * Main.rand.NextFloat(2f, 10f),
                    Color.LightCyan, Color.SlateBlue, Main.rand.Next(40, 120), Main.rand.NextFloat(.4f, .6f), Main.rand.NextFloat(.6f, 1f));
            }
            Dust.NewDustPerfect(pos, DustID.SilverCoin, Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(4f, 8f), 0, default, Main.rand.NextFloat(.6f, .8f));
            ParticleRegistry.SpawnGlowParticle(pos, Projectile.velocity * Main.rand.NextFloat(4f, 10f), Main.rand.Next(18, 30), Main.rand.NextFloat(.4f, .7f), Color.SlateBlue);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Parried)
        {
            ParryEffects();

            if (target.damage > 0)
            {
                Owner.GiveIFrames(35);
                Owner.velocity += target.SafeDirectionTo(Owner.Center) * 8f;
            }

            Parried = true;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Projectile.DrawProjectileBackglow(Color.DeepSkyBlue, 4f * Projectile.scale, (byte)(100 * Projectile.Opacity));
        Main.spriteBatch.Draw(tex, pos, null, Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * Projectile.Opacity, Projectile.rotation, tex.Size() / 2, Projectile.scale, 0, 0f);
        return false;
    }
}
