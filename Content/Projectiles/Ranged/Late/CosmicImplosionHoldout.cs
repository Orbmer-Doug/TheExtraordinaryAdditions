using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class CosmicImplosionHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CosmicImplosionHoldout);
    public override int AssociatedItemID => ModContent.ItemType<CosmicImplosion>();
    public override int IntendedProjectileType => ModContent.ProjectileType<CosmicImplosionHoldout>();

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 0;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = 2f;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float PullTime => ref Projectile.ai[1];
    public ref float ReleaseTime => ref Projectile.ai[2];
    public bool Released
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public static readonly int PullbackTime = SecondsToFrames(1.4f);
    public static readonly int FireTime = SecondsToFrames(.4f);

    public Vector2 CurrentEnd;
    public override void WriteExtraAI(BinaryWriter writer) => writer.WriteVector2(CurrentEnd);
    public override void GetExtraAI(BinaryReader reader) => CurrentEnd = reader.ReadVector2();
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = Center + PolarVector(25f * Projectile.scale, Projectile.rotation) + PolarVector(10f * Projectile.scale * Dir * Owner.gravDir, Projectile.rotation - PiOver2);
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        if (String == null || String.Disposed)
            String = new(c => 3f, (c, pos) => Color.Cyan.Lerp(Color.Fuchsia, c.X), null, 40);

        Vector2 cyanCenter = Projectile.Center + PolarVector(11f * Projectile.scale, Projectile.rotation) + PolarVector(16f * Projectile.scale * Dir * Owner.gravDir, Projectile.rotation - PiOver2);
        Vector2 start = Projectile.Center + PolarVector(-10f * Projectile.scale, Projectile.rotation) + PolarVector(8f * Projectile.scale * Dir * Owner.gravDir, Projectile.rotation - PiOver2);
        Vector2 end = Projectile.Center + PolarVector(12f * Projectile.scale, Projectile.rotation) + PolarVector(6f * Projectile.scale * Dir * Owner.gravDir, Projectile.rotation - PiOver2);
        Vector2 purpleCenter = Projectile.Center + PolarVector(9f * Projectile.scale, Projectile.rotation) + PolarVector(11f * Projectile.scale * Dir * Owner.gravDir, Projectile.rotation + PiOver2);

        float anim = Animators.MakePoly(3f).InFunction(InverseLerp(0f, PullbackTime, PullTime));
        if (Released)
        {
            float comp = InverseLerp(0f, FireTime, ReleaseTime);
            anim = Animators.Elastic.OutFunction(comp);

            if (ReleaseTime == 0f)
            {
                AdditionsSound.etherealRelease2.Play(end, 1.2f, -.2f, .1f);
                if (this.RunLocal())
                    Projectile.NewProj(end, Projectile.velocity * 10f, ModContent.ProjectileType<EmpyreanRipshot>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            }

            ReleaseTime++;

            if (comp == 1f)
            {
                ReleaseTime = 0f;
                Released = false;
                anim = 0f;
                this.Sync();
            }
        }
        else if (PullTime < PullbackTime)
        {
            PullTime++;
        }
        else if (PullTime >= PullbackTime)
        {
            if (this.RunLocal() && Modded.SafeMouseLeft.Current)
            {
                PullTime = 0f;
                Released = true;
                this.Sync();
            }
        }

        CurrentEnd = !Released ? Vector2.Lerp(end, start, anim) : Vector2.Lerp(start, end, anim);

        for (int i = 0; i < 40; i++)
            Points.SetPoint(i, MultiLerp(InverseLerp(0f, 40f, i), cyanCenter, CurrentEnd, purpleCenter));

        Time++;
    }

    public OptimizedPrimitiveTrail String;
    public TrailPoints Points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 origin = texture.Size() / 2;
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection());

        void draw()
        {
            if (String != null && Points != null)
                String.DrawTrail(ShaderRegistry.StandardPrimitiveShader, Points.Points, 100, true, false);
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.Dusts);

        return false;
    }
}
