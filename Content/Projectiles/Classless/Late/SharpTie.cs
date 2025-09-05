
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class SharpTie : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SharpTie);

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 4;
        Projectile.timeLeft = 1000;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.aiStyle = 0;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }

    public ref float Timer => ref Projectile.ai[0];
    public override void AI()
    {
        after ??= new(10, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 10));

        Projectile.rotation += Projectile.direction * 0.3f;
        Lighting.AddLight(Projectile.Center, Color.WhiteSmoke.ToVector3() * .6f);

        if (Timer % 2f == 0f)
        {
            Vector2 pos = Projectile.RandAreaInEntity();
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.15f) * .8f;
            Color c1 = Color.White;
            float scale = Main.rand.NextFloat(.3f, .7f);
            ParticleRegistry.SpawnSparkParticle(pos, vel, 30, scale, c1);
        }

        NPC target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 500, true, true));
        if (target != null && Timer > 20)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 18f, 0.1f);
        }
        else
        {
            if (Projectile.velocity.Length() < 30f)
                Projectile.velocity.Y -= .5f;
        }

        Timer++;
    }
}