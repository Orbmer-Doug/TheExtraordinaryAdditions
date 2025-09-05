using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class CryingEye : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CryingEye);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = -1f;
        ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 1000;
        ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 20f;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 16; // The width of the projectile's hitbox.
        Projectile.height = 16; // The height of the projectile's hitbox.

        Projectile.aiStyle = ProjAIStyleID.Yoyo; // The projectile's ai style. Yoyos use aiStyle 99 (ProjAIStyleID.Yoyo). A lot of yoyo code checks for this aiStyle to work properly.

        Projectile.friendly = true; // Player shot projectile. Does damage to enemies but not to friendly Town NPCs.
        Projectile.DamageType = DamageClass.MeleeNoSpeed; // Benefits from melee bonuses. MeleeNoSpeed means the item will not scale with attack speed.
        Projectile.penetrate = -1; // All vanilla yoyos have infinite penetration. The number of enemies the yoyo can hit before being pulled back in is based on YoyosLifeTimeMultiplier.
    }
    public override void AI()
    {
        Projectile.rotation += .3f;
        Vector2 val = Projectile.position - Main.player[Projectile.owner].position;
        if (Projectile.Additions().ExtraAI[0]++ % 10f == 0f)
        {
            Projectile.NewProj(Projectile.Center, Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(3f, 9f), ModContent.ProjectileType<CryingTear>(), Projectile.damage / 2, 0f, Projectile.owner);
        }
        if (val.Length() > 3200f)
        {
            Projectile.Kill();
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(103, 180);
    }
    // notes for aiStyle 99: 
    // localAI[0] is used for timing up to YoyosLifeTimeMultiplier
    // localAI[1] can be used freely by specific types
    // ai[0] and ai[1] usually point towards the x and y world coordinate hover point
    // ai[0] is -1f once YoyosLifeTimeMultiplier is reached, when the player is stoned/frozen, when the yoyo is too far away, or the player is no longer clicking the shoot button.
    // ai[0] being negative makes the yoyo move back towards the player
    // Any AI method can be used for dust, spawning projectiles, etc specific to your yoyo.

    public override void PostAI()
    {
        if (Main.rand.NextBool(4))
        {
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.Water, Vector2.UnitY * 4f, 0, default, Main.rand.NextFloat(.5f, 1.6f));
        }
    }
}