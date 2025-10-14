using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class JudgementHammer : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JudgementHammer);

    public override void SetDefaults()
    {
        Projectile.width = 112;
        Projectile.height = 128;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.MaxUpdates = 4;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool Free
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }

    public ref float RotationOffset => ref Projectile.ai[2];
    public SpriteEffects Effects
    {
        get => (SpriteEffects)Projectile.spriteDirection;
        set => Projectile.spriteDirection = (int)value;
    }
    public int Direction
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }
    public static readonly float SwingAngle = TwoPi / 3f;
    public float ReelCompletion => InverseLerp(0f, Asterlin.Cleave_HammerReelTime, ModOwner.AITimer);

    public override void SendAI(BinaryWriter writer)
    {
        writer.Write((float)Projectile.rotation);
        writer.Write((sbyte)Projectile.spriteDirection);
        writer.Write((int)Projectile.MaxUpdates);
    }

    public override void ReceiveAI(BinaryReader reader)
    {
        Projectile.rotation = (float)reader.ReadSingle();
        Projectile.spriteDirection = (sbyte)reader.ReadSByte();
        Projectile.MaxUpdates = (int)reader.ReadInt32();
    }

    public override bool? CanDamage() => Free ? null : false;
    public override bool ShouldUpdatePosition() => Free;

    public RotatedRectangle Rect()
    {
        return new(100f, Projectile.Center, Projectile.Center + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 148f);
    }

    public override void SafeAI()
    {
        if (!Free)
        {
            Projectile.timeLeft = 400;
            Projectile.Center = ModOwner.RightHandPosition;
            Projectile.rotation = ModOwner.RightArm.RootPosition.AngleTo(ModOwner.RightHandPosition) + MathHelper.PiOver4;
            Direction = -ModOwner.Direction;
        }
        else
        {
            after ??= new(12, () => Projectile.Center);
            after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, 1f, Projectile.rotation, Effects, 0, 2, 0f, null, false, 0f));
            Projectile.VelocityBasedRotation(.01f);
            Projectile.velocity.X *= .991f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -40f, 40f);
        }

        Projectile.Center = Projectile.Center.ClampInWorld();
        Time++;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        if (Free)
            target.KillMe(PlayerDeathReason.ByCustomReason(GetNetworkText($"Status.Death.AsterlinDeath3", target.name)), int.MaxValue, Direction);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 origin;

        if (!Free)
        {
            if (Direction == 1)
            {
                origin = new Vector2(0, tex.Height);

                RotationOffset = 0f;
                Effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(tex.Width, tex.Height);

                RotationOffset = PiOver2;
                Effects = SpriteEffects.FlipHorizontally;
            }
        }
        else
            origin = tex.Size() / 2;

        after?.DrawFancyAfterimages(tex, [Color.LightGoldenrodYellow, Color.Gold, Color.DarkGoldenrod]);
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = ((MathHelper.TwoPi * InverseLerp(0f, 8, i) + Main.GlobalTimeWrappedHourly * Utils.Remap(j, 0, 3, 4f, 1.8f)).ToRotationVector2() * Utils.Remap(j, 0, 3, 5f, 25f));
                Color color = MulticolorLerp(InverseLerp(0f, 3, j), Color.PaleGoldenrod, Color.Gold, Color.DarkGoldenrod) with { A = 0 } * ReelCompletion * Utils.Remap(j, 0, 3, .9f, .3f);
                Main.spriteBatch.Draw(tex, Projectile.Center + offset - Main.screenPosition, null, color,
                Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
            }
        }
        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White,
                    Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        return false;
    }
}