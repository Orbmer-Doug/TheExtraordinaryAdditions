using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HailfireShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HailfireShell);
    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.width = 22;
        Projectile.height = 56;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.extraUpdates = 0;
        Projectile.timeLeft = 10000;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
        Projectile.hide = true;
    }

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(offset);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        offset = reader.ReadVector2();
    }

    public bool Stuck
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public bool HitGround
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.Additions().ExtraAI[5];
    public ref float Blink => ref Projectile.Additions().ExtraAI[6];
    public ref float EnemyID => ref Projectile.ai[1];

    public static readonly float MaxTime = SecondsToFrames(5f);

    public override void AI()
    {
        after ??= new(7, () => Projectile.Center);

        // Increment
        if (Time < MaxTime)
            Time++;

        // Make the beeps
        if (Time % 60f == 59f)
        {
            AdditionsSound.WarningBeep.Play(Projectile.Center, .8f, 0f, .2f);
            Blink = 10f;
        }

        // Stick to enemies if necessary
        if (Stuck)
        {
            NPC target = Main.npc[(int)EnemyID];

            if (target == null || target.active == false)
                return;

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 120;
                Projectile.position = target.position + offset;
            }
        }

        // Otherwise fall when not in ground
        else if (!HitGround)
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .28f, -20f, 20f);
            Projectile.FacingUp();
        }
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255, 0, 0f, null, false, .4f));

        if (Owner.Additions().SafeMouseRight.JustPressed && this.RunLocal())
        {
            Projectile.Kill();
        }

        if (Blink > 0f)
        {
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * .7f);
            Blink--;
        }

        if (Time.BetweenNum(0f, MaxTime * .25f))
        {
            Projectile.damage = Hailfire.Damage;
        }
        if (Time.BetweenNum(0f, MaxTime * .5f))
        {
            Projectile.damage = (int)(Hailfire.Damage * 1.9f);
        }
        if (Time.BetweenNum(0f, MaxTime * .75f))
        {
            Projectile.damage = (int)(Hailfire.Damage * 2.6f);
        }
        if (Time == MaxTime)
        {
            Projectile.damage = (int)(Hailfire.Damage * 3f);
        }
    }

    public override void OnKill(int timeLeft)
    {
        ScreenShakeSystem.New(new(Utils.Remap(Time, 0f, MaxTime, 0f, .4f), .4f), Projectile.Center);

        AdditionsSound.crosscodeExplosion.Play(Projectile.Center, .8f, 0f, .1f, 10, Name);

        if (this.RunLocal())
        {
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HailfireExplosion>(), Projectile.damage, 0f, Projectile.owner, (int)(Time * 1.2f), 0f);
        }
    }

    public override bool? CanHitNPC(NPC target) => !HitGround;
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.Center += oldVelocity * 2f;
        Projectile.velocity = Vector2.Zero;

        if (!HitGround)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Pitch = -.25f, Volume = .9f }, Projectile.Center);
            Collision.HitTiles(Projectile.Center, oldVelocity, Projectile.width, Projectile.height);
            HitGround = true;
        }

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Set the sticking variables
        if (Stuck == false && target.life > 0)
        {
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            Stuck = true;
        }
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (Projectile.ai[1] < 0f)
        {
            behindNPCsAndTiles.Add(index);
        }
        else
        {
            Projectile.hide = false;
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color drawCol = Blink > 0f ? Projectile.GetAlpha(Color.Red with { A = 255 }) : Projectile.GetAlpha(lightColor);
        after?.DrawFancyAfterimages(texture, [lightColor]);
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, drawCol, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction);

        Main.spriteBatch.EnterShaderRegion();
        Texture2D telegraphBase = AssetRegistry.InvisTex;
        ManagedShader circle = ShaderRegistry.CircularAoETelegraph;
        float interpolant = InverseLerp(0f, MaxTime, Time);
        circle.TrySetParameter("opacity", interpolant * .67f);
        circle.TrySetParameter("color", Color.Lerp(Color.DarkRed, Color.Red, MathF.Pow(Sin01(Main.GlobalTimeWrappedHourly * 2f), 2)) * interpolant);
        circle.TrySetParameter("secondColor", Color.Lerp(Color.DarkOrange, Color.OrangeRed, interpolant));
        circle.Render();

        Main.spriteBatch.DrawBetterRect(telegraphBase, ToTarget(Projectile.Center, Vector2.One * Time * 1.2f), null, Color.White, 0f, telegraphBase.Size() / 2f);
        Main.spriteBatch.ExitShaderRegion();

        return false;
    }
}
