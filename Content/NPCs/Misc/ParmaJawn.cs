using Terraria;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Misc;

public class ParmaJawn : ProjOwnedByNPC<TheGiantSnailFromAncientTimes>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ParmaJawn);
    public override bool IgnoreOwnerActivity => true;

    public override void SetDefaults()
    {
        Projectile.width = 97; Projectile.height = 148;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
    }

    public override void SafeAI()
    {
        Projectile.VelocityBasedRotation();
        Projectile.velocity.Y += .2f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.Kill();
        return true;
    }

    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 300f, Vector2.Zero, 30, Color.White, 0f, Color.LightGray);

        Projectile.ExpandHitboxBy(300);
        Projectile.Damage();

        AdditionsSound.BlueBerryBUFFINS.Play(Projectile.Center, Main.rand.NextFloat(1f, 1.6f), 0f, 9.4f, 0, Name);
    }
}
