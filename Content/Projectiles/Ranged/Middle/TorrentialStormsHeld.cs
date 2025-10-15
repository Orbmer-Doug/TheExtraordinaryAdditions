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
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;


namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class TorrentialStormsHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TorrentialStormsHeld);
    public override int AssociatedItemID => ModContent.ItemType<TorrentialStorms>();
    public override int IntendedProjectileType => ModContent.ProjectileType<TorrentialStormsHeld>();

    public override void Defaults()
    {
        Projectile.width = 34;
        Projectile.height = 62;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public List<Vector2> Points;
    public TrailPoints cache = new(MaxPoints);
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

    public override void SafeAI()
    {
        Item ammoItem = Owner.ChooseAmmo(Item);
        Texture2D arrow = ammoItem != null ? ammoItem.ThisItemTexture() : AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        if (trail == null || trail.Disposed)
            trail = new(c => 14f, (c, pos) => MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly * 1.2f) + c.X, new(77, 89, 151), new(109, 119, 169), new(143, 152, 203)), null, 150);

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        }
        Projectile.Center = Center + PolarVector(20f, Projectile.rotation) + PolarVector(10f * Dir, Projectile.rotation + MathHelper.PiOver2);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Dir);

        float reel = InverseLerp(0f, ReelTime, Time);
        float close = InverseLerp(0f, 22f, Time);

        float armRot = Projectile.rotation + (.62f * Dir);
        float reelAnim = Animators.MakePoly(2.2f).InFunction.Evaluate(armRot, armRot + (.65f * Dir), Switch != 0 ? reel : OldStringCompletion);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, reelAnim - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + .2f * Dir - MathHelper.PiOver2);

        Vector2 centerString = PolarVector(12f, Projectile.rotation - MathHelper.Pi);
        Vector2 drawBack = PolarVector(ReelDist * StringCompletion, Projectile.rotation - MathHelper.Pi);

        Vector2 top = Projectile.RotHitbox().Center + PolarVector(18f, Projectile.rotation - MathHelper.PiOver2) + centerString;
        Vector2 middle = Projectile.RotHitbox().Center + centerString + drawBack;
        arrowPos = middle + PolarVector(arrow.Height / 2, Projectile.rotation);
        Vector2 bottom = Projectile.RotHitbox().Center + PolarVector(-18f, Projectile.rotation - MathHelper.PiOver2) + centerString;

        Points = [];
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
                        if (this.RunLocal())
                        {
                            Projectile.NewProj(arrowPos, Projectile.velocity * speed, type, dmg, kb, Owner.whoAmI);
                        }

                        if (this.RunLocal())
                        {
                            Vector2 pos = Center;
                            float mouseXDist = Main.mouseX + Main.screenPosition.X - pos.X;
                            float mouseYDist = Main.mouseY - pos.Y;
                            if (Owner.gravDir == -1f)
                                mouseYDist = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - pos.Y;
                            float arrowSpeed = speed;
                            float mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
                            if ((float.IsNaN(mouseXDist) && float.IsNaN(mouseYDist)) || (mouseXDist == 0f && mouseYDist == 0f))
                            {
                                mouseXDist = Owner.direction;
                                mouseYDist = 0f;
                                mouseDistance = arrowSpeed;
                            }
                            else
                            {
                                mouseDistance = arrowSpeed / mouseDistance;
                            }

                            pos = new Vector2(Owner.position.X + Owner.width / 2 + (Main.rand.Next(201)
                                * -(float)Owner.direction) + (Main.mouseX + Main.screenPosition.X - Owner.position.X), Owner.MountedCenter.Y - 600f);
                            pos.X = (pos.X + Owner.Center.X) / 2f + Main.rand.Next(-200, 201);
                            pos.Y -= 100f;
                            mouseXDist = Main.mouseX + Main.screenPosition.X - pos.X;
                            mouseYDist = Main.mouseY + Main.screenPosition.Y - pos.Y;
                            if (mouseYDist < 0f)
                            {
                                mouseYDist *= -1f;
                            }
                            if (mouseYDist < 20f)
                            {
                                mouseYDist = 20f;
                            }
                            mouseDistance = MathF.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
                            mouseDistance = arrowSpeed / mouseDistance;
                            mouseXDist *= mouseDistance;
                            mouseYDist *= mouseDistance;
                            Vector2 vel = new Vector2(mouseXDist, mouseYDist).SafeNormalize(Vector2.Zero).RotatedByRandom(.35f * (1f - reel));
                            Projectile.NewProj(pos, vel, ModContent.ProjectileType<TorrentialLightning>(), dmg, kb, Owner.whoAmI, 0f, reel);
                            AdditionsSound.LightningStrike.Play(pos, MathHelper.Max(.2f, reel) * 2f, -MathHelper.Max(.2f, reel) * .4f);
                        }
                        SoundEngine.PlaySound(SoundID.Item5 with { Volume = Main.rand.NextFloat(.9f, 1.2f), PitchVariance = .1f, Identifier = Name }, arrowPos);

                        for (int i = 0; i < (reel < .33f ? 2 : reel < .66f ? 3 : 4); i++)
                        {
                            Vector2 offset = Main.rand.NextVector2CircularLimited(70f, 70f, .4f, 1f);
                            Vector2 posit = arrowPos + offset;
                            if (this.RunLocal())
                            {
                                Vector2 veloc = posit.SafeDirectionTo(Modded.mouseWorld + offset) * speed * Main.rand.NextFloat(.6f, 1.25f);
                                Projectile.NewProj(posit, veloc, ModContent.ProjectileType<RainDrop>(), dmg / 3, kb / 3, Owner.whoAmI);
                                ParticleRegistry.SpawnPulseRingParticle(posit, veloc.SafeNormalize(Vector2.Zero), Main.rand.Next(35, 50), veloc.ToRotation(), new(.5f, 1f), 0f, 30f, Color.CornflowerBlue);
                                for (int j = 0; j < 12; j++)
                                    Dust.NewDustPerfect(posit, DustID.Water, veloc.RotatedByRandom(.2f) * Main.rand.NextFloat(.4f, .8f), 0, default, Main.rand.NextFloat(1.5f, 1.9f)).noGravity = true;
                            }
                        }

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
        Item ammoItem = Owner.ChooseAmmo(Item);
        Texture2D arrow = ammoItem != null ? ammoItem.ThisItemTexture() : AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        void draw()
        {
            if (trail == null || trail.Disposed || cache == null)
                return;
            ManagedShader shader = ShaderRegistry.EnlightenedBeam;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakLightning), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Lightning2), 2);
            trail.DrawTrail(shader, cache.Points, 150);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);

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
