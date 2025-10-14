using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class BurstingLight : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2500;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public const int MaxDist = 1000;
    public const int BurstTime = 40;
    public int TotalTime => 18 + Asterlin.RotatedDicing_TelegraphTime;
    public float TeleCompletion => (float)Asterlin.RotatedDicing_TelegraphTime / TotalTime;

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public Vector2 Size;
    public override void SendAI(BinaryWriter writer) => writer.WriteVector2(Size);
    public override void ReceiveAI(BinaryReader reader) => Size = reader.ReadVector2();
    public override void SafeAI()
    {
        if (Time > TotalTime)
            Kill();

        if (Time == Asterlin.RotatedDicing_TelegraphTime)
            AdditionsSound.etherealHitCrunch.Play(Owner.Center, 1.8f, .1f, 0f, 1, Name);

        Projectile.rotation = Projectile.velocity.ToRotation();
        Size.X = (int)new Animators.PiecewiseCurve()
            .AddStall(32f, TeleCompletion)
            .Add(32f, 10000, 1f, Animators.MakePoly(4f).OutFunction)
            .Evaluate(InverseLerp(0f, TotalTime, Time));
        Size.Y = (int)new Animators.PiecewiseCurve()
            .AddStall(32f, TeleCompletion)
            .Add(32f, 0f, 1f, Animators.MakePoly(2f).InOutFunction)
            .Evaluate(InverseLerp(0f, TotalTime, Time));
        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Time >= Asterlin.RotatedDicing_TelegraphTime ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 start = Projectile.Center;
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
        return targetHitbox.LineCollision(start, start + dir * Size.X / 2, Size.Y * .55f) || targetHitbox.LineCollision(start, start - dir * Size.X / 2, Size.Y * .55f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            float telegraphCompletion = InverseLerp(0f, Asterlin.RotatedDicing_TelegraphTime, Time);
            for (float i = .6f; i < 1.2f; i += .05f)
            {
                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                float anim = Animators.MakePoly(4f).InFunction.Evaluate(2f, 1f, telegraphCompletion);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size * i * anim), null, Color.PaleGoldenrod.Lerp(Color.DarkGoldenrod, i), Projectile.rotation, tex.Size() / 2f);
            }

            Texture2D cap = AssetRegistry.GetTexture(AdditionsTexture.BloomLineCap);
            Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
            float fade = GetLerpBump(0f, .2f, 1f, .8f, telegraphCompletion);
            float dist = 10000;
            Vector2 a = Projectile.Center - dir * dist;
            Vector2 b = Projectile.Center + dir * dist;
            Vector2 tangent = a.SafeDirectionTo(b) * a.Distance(b);
            float rotation = tangent.ToRotation();
            const float ImageThickness = 6;
            float thicknessScale = 4f / ImageThickness;
            Vector2 capOrigin = new(cap.Width, cap.Height / 2f);
            Vector2 middleOrigin = new(0, horiz.Height / 2f);
            Vector2 middleScale = new(a.Distance(b) / horiz.Width, thicknessScale);
            Color color = Color.PaleGoldenrod * fade;
            Main.spriteBatch.Draw(horiz, a - Main.screenPosition, null, color, rotation, middleOrigin, middleScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(cap, a - Main.screenPosition, null, color, rotation, capOrigin, thicknessScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(cap, b - Main.screenPosition, null, color, rotation + MathHelper.Pi, capOrigin, thicknessScale, SpriteEffects.None, 0f);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
        return false;
    }
}
