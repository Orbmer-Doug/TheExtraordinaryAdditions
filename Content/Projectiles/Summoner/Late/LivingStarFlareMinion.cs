using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class LivingStarFlareMinion : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public NPC Target => NPCTargeting.MinionHoming(new(Projectile.Center, 1050, false, true), Owner);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 8;

        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 12000;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.minionSlots = 2f;
        Projectile.penetrate = -1;
        Projectile.width =
        Projectile.height = 32;
        Projectile.scale = 0;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.netImportant = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.minion = true;
    }

    public ref float Timer => ref Projectile.ai[0];
    public ref float PhaseTimer => ref Projectile.ai[1];
    public ref float CurrentPhase => ref Projectile.ai[2];

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Owner.AddBuff(ModContent.BuffType<LittleStar>(), 2, true, false);
            Projectile.localAI[0] = 1f;
        }

        if (Target != null)
        {
            Timer++;
        }

        if (Target == null)
        {
            GenericStance();
            Timer = 0f;
        }

        CheckActive(Owner);
    }

    private void GenericStance()
    {

    }

    private bool CheckActive(Player owner)
    {
        if (owner.dead || !owner.active)
        {
            owner.ClearBuff(ModContent.BuffType<LittleStar>());

            return false;
        }

        if (owner.HasBuff(ModContent.BuffType<LittleStar>()))
        {
            Projectile.timeLeft = 2;
        }

        return true;
    }

    public void Star()
    {
        Texture2D invis = AssetRegistry.InvisTex;
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.FractalNoise);
        ManagedShader fireball = ShaderRegistry.FireballShader;

        fireball.TrySetParameter("sampleTexture2", noise);
        fireball.TrySetParameter("mainColor", Color.Lerp(Color.Goldenrod, Color.Gold, 0.3f).ToVector3());
        fireball.TrySetParameter("resolution", new Vector2(250f, 250f));
        fireball.TrySetParameter("speed", 0.96f);
        fireball.TrySetParameter("zoom", 0.0004f);
        fireball.TrySetParameter("opacity", Projectile.Opacity);

        float[] scaleFactors =
        [
                1f, 0.8f, 0.7f, 0.57f, 0.44f, 0.32f, 0.22f
        ];
        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        for (int i = 0; i < scaleFactors.Length; i++)
        {
            fireball.TrySetParameter("time", Main.GlobalTimeWrappedHourly * (i * 0.04f + 0.32f));
            fireball.Render();
            Main.spriteBatch.Draw(invis, drawPos, null, Color.White, Projectile.rotation, invis.Size() * 0.5f, Projectile.scale * 150f * scaleFactors[i], SpriteEffects.None, 0f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        PixelationSystem.QueueTextureRenderAction(Star, PixelationLayer.OverPlayers, null, ShaderRegistry.FireballShader);
        return false;
    }
}
