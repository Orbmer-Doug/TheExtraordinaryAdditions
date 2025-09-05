using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;


namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class DownpourHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DownpourHeld);
    public override int AssociatedItemID => ModContent.ItemType<Downpour>();
    public override int IntendedProjectileType => ModContent.ProjectileType<DownpourHeld>();

    public override void Defaults()
    {
        Projectile.width = 28;
        Projectile.height = 60;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public List<Vector2> Points = [];
    public ManualTrailPoints cache;
    public const int MaxPoints = 40;
    public const float ReelDist = 15f;
    public int ReelTime => Item.useTime;
    public ref float Time => ref Projectile.ai[0];
    public ref float Switch => ref Projectile.ai[1];
    public ref float StringCompletion => ref Projectile.ai[2];
    public ref float OldStringCompletion => ref Projectile.Additions().ExtraAI[0];
    public ref float TotalTime => ref Projectile.Additions().ExtraAI[2];
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

    public override void SafeAI()
    {
        Item ammoItem = Owner.ChooseAmmo(Item);
        Texture2D arrow = ammoItem != null ? ammoItem.ThisItemTexture() : AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Owner.heldProj = Projectile.whoAmI;
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        }
        Projectile.Center = Center + PolarVector(10f, Projectile.rotation) + PolarVector(10f * Dir, Projectile.rotation + MathHelper.PiOver2);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Dir);

        float reel = InverseLerp(0f, ReelTime, Time);
        float close = InverseLerp(0f, 22f, Time);

        float armRot = Projectile.rotation + (.595f * Dir);
        float reelAnim = Animators.MakePoly(2.2f).InFunction.Evaluate(armRot, armRot + (.65f * Dir), Switch != 0 ? reel : OldStringCompletion);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, reelAnim - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + .2f * Dir - MathHelper.PiOver2);

        Vector2 centerString = PolarVector(3f, Projectile.rotation - MathHelper.Pi);
        Vector2 drawBack = PolarVector(ReelDist * StringCompletion, Projectile.rotation - MathHelper.Pi);

        Vector2 top = Projectile.RotHitbox().Center + PolarVector(13f, Projectile.rotation - MathHelper.PiOver2) + centerString;
        Vector2 middle = Projectile.RotHitbox().Center + centerString + drawBack;
        arrowPos = middle + PolarVector(arrow.Height / 2, Projectile.rotation);
        Vector2 bottom = Projectile.RotHitbox().Center + PolarVector(-12f, Projectile.rotation - MathHelper.PiOver2) + centerString;

        Points = [];
        cache ??= new(MaxPoints);
        for (int i = 0; i < MaxPoints; i++)
            Points.Add(MultiLerp(InverseLerp(0f, MaxPoints, i), top, middle, bottom));
        cache.SetPoints(Points);

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
                    if (reel >= 1f || (this.RunLocal() && !Modded.MouseLeft.Current))
                    {
                        Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammo, Owner.ShouldConsumeAmmo(Item));
                        speed *= reel;
                        dmg = (int)(dmg * MathHelper.Clamp(reel, .1f, 1f));
                        kb *= reel;
                        Projectile.NewProj(arrowPos, Projectile.velocity * speed, type, dmg, kb, Owner.whoAmI);

                        for (int i = 0; i < (reel < .33f ? 1 : reel < .66f ? 2 : 3); i++)
                        {
                            Vector2 offset = Main.rand.NextVector2CircularLimited(60f, 60f, .6f, 1f);
                            Vector2 pos = arrowPos + offset; 
                            if (this.RunLocal())
                            {
                                Vector2 vel = pos.SafeDirectionTo(Modded.mouseWorld + offset) * speed * Main.rand.NextFloat(.8f, 1.3f);
                                Projectile.NewProj(pos, vel, ModContent.ProjectileType<RainDrop>(), dmg / 3, kb / 3, Owner.whoAmI);
                                ParticleRegistry.SpawnPulseRingParticle(pos, vel.SafeNormalize(Vector2.Zero), Main.rand.Next(20, 30), vel.ToRotation(), new(.5f, 1f), 0f, 30f, Color.CornflowerBlue);
                                for (int j = 0; j < 6; j++)
                                {
                                    Dust.NewDustPerfect(pos, DustID.Water, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.4f, .8f), 0, default, Main.rand.NextFloat(1.5f, 1.9f)).noGravity = true;
                                }
                            }
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

    public override bool PreDraw(ref Color lightColor)
    {
        Item ammoItem = Owner.ChooseAmmo(Item);
        Texture2D arrow = ammoItem != null ? ammoItem.ThisItemTexture() : AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Point p = Projectile.RotHitbox().Center.ToTileCoordinates();
        OptimizedPrimitiveTrail trail = new(c => 2f, (c, pos) => new Color(143, 152, 203) * Lighting.Brightness(p.X, p.Y), null, MaxPoints);
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
