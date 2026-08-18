using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class BurstingLight : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

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

    public int TotalTime => 18 + Asterlin.RotatedDicing_TelegraphTime;
    public float TeleCompletion => (float) Asterlin.RotatedDicing_TelegraphTime / TotalTime;

    public int Time
    {
        get => (int) Projectile.ai[0];
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
            AssetRegistry.GennedSounds.etherealHitCrunch.Play(Owner.Center, 1.8f, .1f, 0f, 1, Name);

        Projectile.rotation = Projectile.velocity.ToRotation();
        Size.X = (int) new PiecewiseCurve()
            .AddStall(32f, TeleCompletion)
            .Add(32f, 10000, 1f, MakePoly(4f).OutFunction)
            .Evaluate(InverseLerp(0f, TotalTime, Time));
        Size.Y = (int) new PiecewiseCurve()
            .AddStall(32f, TeleCompletion)
            .Add(32f, 0f, 1f, MakePoly(2f).InOutFunction)
            .Evaluate(InverseLerp(0f, TotalTime, Time));
        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Time >= Asterlin.RotatedDicing_TelegraphTime ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 start = Projectile.Center;
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
        return targetHitbox.LineCollision(start, start + dir * Size.X / 2, Size.Y * .55f) ||
               targetHitbox.LineCollision(start, start - dir * Size.X / 2, Size.Y * .55f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}
