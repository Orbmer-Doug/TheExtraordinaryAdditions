using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;


namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class BowOfGreekFlamesHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BowOfGreekFlamesHeld);
    public override int AssociatedItemID => ModContent.ItemType<BowOfGreekFlames>();
    public override int IntendedProjectileType => ModContent.ProjectileType<BowOfGreekFlamesHeld>();

    public override void Defaults()
    {
        Projectile.width = 24;
        Projectile.height = 42;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public List<Vector2> Points;
    public TrailPoints cache;
    public const int MaxPoints = 40;
    public const float ReelDist = 15f;
    public int ReelTime => Item.useTime;
    public ref float Time => ref Projectile.ai[0];
    public ref float Switch => ref Projectile.ai[1];
    public ref float StringCompletion => ref Projectile.ai[2];
    public ref float OldStringCompletion => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float TotalTime => ref Projectile.AdditionsInfo().ExtraAI[2];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void OnSpawn(IEntitySource source)
    {
        Switch = -1;
        Projectile.netUpdate = true;
    }

    public Vector2 arrowPos;
    public override void WriteExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(arrowPos);
        writer.Write(Projectile.rotation);
    }
    public override void GetExtraAI(BinaryReader reader)
    {
        arrowPos = reader.ReadVector2();
        Projectile.rotation = reader.ReadSingle();
    }

    public static readonly Texture2D arrow = AssetRegistry.GetTexture(AdditionsTexture.GreekBombArrow);
    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => 2f, (c, pos) => new(219, 255, 186), null, MaxPoints);

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        }
        Projectile.Center = Center + PolarVector(Projectile.width / 2 + 5f, Projectile.rotation) + PolarVector(10f * Dir, Projectile.rotation + MathHelper.PiOver2);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();
        Owner.ChangeDir(Dir);
        float reel = InverseLerp(0f, ReelTime, Time);
        float close = InverseLerp(0f, 22f, Time);

        float armRot = Projectile.rotation + (.72f * Dir);
        float reelAnim = Animators.MakePoly(2.2f).InFunction.Evaluate(armRot, armRot + (.7f * Dir), Switch != 0 ? reel : OldStringCompletion);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, reelAnim - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + .2f * Dir - MathHelper.PiOver2);

        Vector2 centerString = PolarVector(9f, Projectile.rotation - MathHelper.Pi);
        Vector2 drawBack = PolarVector(ReelDist * StringCompletion, Projectile.rotation - MathHelper.Pi);

        Vector2 top = Projectile.RotHitbox().Center + PolarVector(18f, Projectile.rotation - MathHelper.PiOver2) + centerString;
        Vector2 middle = Projectile.RotHitbox().Center + centerString + drawBack;
        arrowPos = middle + PolarVector(arrow.Height / 2, Projectile.rotation);
        Vector2 bottom = Projectile.RotHitbox().Center + PolarVector(-18f, Projectile.rotation - MathHelper.PiOver2) + centerString;

        Points = [];
        cache ??= new(MaxPoints);
        for (int i = 0; i < MaxPoints; i++)
            Points.Add(MultiLerp(InverseLerp(0f, MaxPoints, i), top, middle, bottom));
        cache.SetPoints(Points);

        if (Main.rand.NextBool(2))
        {
            ParticleRegistry.SpawnHeavySmokeParticle(top, Vector2.Zero, Main.rand.Next(30, 40), Main.rand.NextFloat(.09f, .14f),
                Color.LawnGreen.Lerp(Color.Lime, Main.rand.NextFloat(.4f, .6f)), Main.rand.NextFloat(.8f, 1.4f));
            ParticleRegistry.SpawnHeavySmokeParticle(bottom, Vector2.Zero, Main.rand.Next(30, 40), Main.rand.NextFloat(.09f, .14f),
                Color.LawnGreen.Lerp(Color.Lime, Main.rand.NextFloat(.4f, .6f)), Main.rand.NextFloat(.8f, 1.4f));
        }

        Item ammoItem = Owner.ChooseAmmo(Item);
        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Switch == -1 && ammoItem != null)
        {
            Switch = 1;
            this.Sync();
        }

        if (Projectile.FinalExtraUpdate())
        {
            switch (Switch)
            {
                case 0:
                    Owner.itemTime = Owner.itemAnimation = 0;
                    StringCompletion = Animators.Elastic.OutFunction.Evaluate(OldStringCompletion, 0f, close);
                    if (close >= 1f)
                    {
                        Switch = -1;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
                case 1:
                    StringCompletion = Animators.MakePoly(2.2f).InFunction.Evaluate(0f, 1f, reel);
                    Lighting.AddLight(arrowPos, Color.LawnGreen.ToVector3() * .7f);
                    if (reel >= 1f || (this.RunLocal() && !Modded.MouseLeft.Current))
                    {
                        Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammo, Owner.ShouldConsumeAmmo(Item));
                        speed *= reel;
                        dmg = (int)(dmg * MathHelper.Clamp(reel, .1f, 1f));
                        kb *= reel;
                        if (this.RunLocal())
                        {
                            Projectile.NewProj(arrowPos, Projectile.velocity * speed, ModContent.ProjectileType<GreekBombArrow>(), dmg, kb, Owner.whoAmI);
                        }
                        SoundEngine.PlaySound(SoundID.Item5 with { Volume = Main.rand.NextFloat(.9f, 1.2f), PitchVariance = .1f, Identifier = Name }, arrowPos);

                        OldStringCompletion = StringCompletion;
                        Switch = 0;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
            }

            if (Switch > -1)
            {
                Time++;
                TotalTime++;
            }
            else
                TotalTime = 0f;
        }
    }

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        if (trail != null && !trail.Disposed && cache != null)
            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 100, false, false);

        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() / 2;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += MathHelper.Pi;
        }
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, direction, 0f);

        float opacity = InverseLerp(0f, 10f, TotalTime);
        if (Switch == 0)
            opacity = 0f;
        Main.spriteBatch.Draw(arrow, arrowPos - Main.screenPosition, null, lightColor * opacity, Projectile.rotation - MathHelper.PiOver2, arrow.Size() / 2, 1f, 0, 0f);

        return false;
    }
}
