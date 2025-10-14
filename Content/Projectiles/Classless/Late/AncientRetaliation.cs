using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class AncientRetaliation : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AncientRetaliation);

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public float RotationAmount => Utils.Remap(Time, 0f, Lifetime / 2, .044f, .001f);
    public int Direction
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public const int Lifetime = 280;
    public override void SetDefaults()
    {
        Projectile.width = 18;
        Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
        Projectile.timeLeft = Lifetime;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation));

        Projectile.FacingUp();

        if (Time <= Lifetime / 2)
        {
            Projectile.velocity = Projectile.velocity.RotatedBy(Direction * RotationAmount) * 1.01f;
        }

        if (Time >= Lifetime / 2)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 35f, .85f);

            Rectangle hitbox = Projectile.Hitbox;
            if (hitbox.Intersects(Owner.Hitbox))
            {
                int dustID = DustID.Sandnado;
                int dustAmt = 4;
                Vector2 dustPos = Projectile.Center - Projectile.velocity / 2f;
                Vector2 dustVel = Projectile.velocity / 4f;
                for (int i = 0; i < dustAmt; i++)
                {
                    Dust obj = Dust.NewDustDirect(dustPos, 0, 0, dustID, 0f, 0f, 0, default, 2.5f);
                    obj.velocity += dustVel;
                    obj.velocity *= Main.rand.NextFloat(0.4f, 1f);
                    obj.noGravity = true;
                }
                Projectile.Kill();
            }
        }
        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 origin = texture.Size() * 0.5f;
        Projectile.DrawProjectileBackglow(Color.Gold, 3f, 90, 10);
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.DarkGoldenrod, Color.Gold, Color.LightYellow], Projectile.Opacity);
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Color.Goldenrod, Projectile.rotation, origin, Projectile.scale, 0);
        return false;
    }
}
