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
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = ProjAIStyleID.Yoyo;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = -1;
    }

    public override void AI()
    {
        Projectile.rotation += .3f;
        Vector2 val = Projectile.position - Main.player[Projectile.owner].position;
        if (Projectile.AdditionsInfo().ExtraAI[0]++ % 10f == 0f && this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(3f, 9f), ModContent.ProjectileType<CryingTear>(), Projectile.damage / 2, 0f, Projectile.owner);
        if (val.Length() > 3200f)
            Projectile.Kill();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(103, 180);
    }

    // notes for yoyo style: 
    // localAI[0] is used for timing up to YoyosLifeTimeMultiplier
    // localAI[1] can be used freely by specific types
    // ai[0] and ai[1] usually point towards the x and y world coordinate hover point
    // ai[0] is -1f once YoyosLifeTimeMultiplier is reached, when the player is stoned/frozen, when the yoyo is too far away, or the player is no longer clicking the shoot button
    // ai[0] being negative makes the yoyo move back towards the player

    public override void PostAI()
    {
        if (Main.rand.NextBool(4))
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.Water, Vector2.UnitY * 4f, 0, default, Main.rand.NextFloat(.5f, 1.6f));
    }
}