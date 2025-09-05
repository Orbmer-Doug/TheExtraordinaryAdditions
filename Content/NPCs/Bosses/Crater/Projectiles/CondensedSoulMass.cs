using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class CondensedSoulMass : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        Projectile.tileCollide = false;
    }

    public LoopedSound Holy;
    public override void SafeAI()
    {
        Holy ??= new(AssetRegistry.GetSound(AdditionsSound.holyLoop), () => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
        Holy.Update(() => Projectile.Center, () => 1.2f, () => 0f);

        Vector2 pos = new(270f, 300f + MathF.Sin(Main.GlobalTimeWrappedHourly * .2f) * 20f);
        Projectile.Center = Owner.Center - pos;
        Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 3f);

        foreach (Player player in Main.ActivePlayers)
        {
            if (player.Center.WithinRange(Projectile.Center, 100f))
            {
                Boss.FightStarted = true;
                Owner.netUpdate = true;
                Projectile.Kill();
            }
        }

        Time += .01f;
    }

    public override void OnKill(int timeLeft)
    {
        AdditionsSound.explosion_large_08.Play(Projectile.Center, 1.5f, -.2f);

        ScreenShakeSystem.New(new(2f, 1f), Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 size = new(300f);
        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D t = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        Main.spriteBatch.DrawBetterRect(t, ToTarget(Projectile.Center, size * 1.5f), null, Color.Gold, 0f, t.Size() / 2, 0);
        Main.spriteBatch.DrawBetterRect(t, ToTarget(Projectile.Center, size * 2.5f), null, Color.Gold, 0f, t.Size() / 2, 0);
        Main.spriteBatch.ResetBlendState();

        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size), null, Color.White, 0f, tex.Size() / 2, 0);
        }
        ManagedShader shader = AssetRegistry.GetShader("SoulMass");
        shader.TrySetParameter("time", Time);
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverPlayers, null, shader);
        return false;
    }
}