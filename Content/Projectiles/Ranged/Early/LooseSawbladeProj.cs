using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class LooseSawbladeProj : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LooseSawblade);

    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.width = 26;
        Projectile.height = 26;
        Projectile.penetrate = 5;
        Projectile.timeLeft = 360;
        Projectile.tileCollide = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float EnemyID => ref Projectile.ai[1];
    public bool HitTile
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(offset);
    public override void ReceiveExtraAI(BinaryReader reader) => offset = reader.ReadVector2();

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        if (Main.rand.NextBool(3))
        {
            int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 0, default(Color), .5f);
            Main.dust[d].position = Projectile.Center;
        }

        if (Projectile.numHits > 0)
        {
            Projectile.knockBack = 0f;

            // Stick to the target
            NPC target = Main.npc[(int)EnemyID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
        }

        else if (Time > 14f && !HitTile)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -22f, 22f);

        Projectile.VelocityBasedRotation();

        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor], Projectile.Opacity);
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.life > 0)
        {
            Projectile.velocity *= 0f;
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;
            this.Sync();
        }

        for (int i = 0; i < 3; i++)
            ParticleRegistry.SpawnBloodParticle(target.RotHitbox().RandomPoint(), Main.rand.NextVector2CircularLimited(11, 11, .2f, .8f), 20, Main.rand.NextFloat(.7f, 1f), Color.DarkRed);
    }

    public override void OnKill(int timeLeft)
    {
        for (int g = 0; g < 40; g++)
        {
            int dust = Dust.NewDust(Projectile.Center, 4, 4, DustID.Bone, 0f, 0f, 100, default);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 3f;
            Main.dust[dust].scale *= 1.2f;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.Center, -oldVelocity * Main.rand.NextFloat(.4f, .9f), Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Dig, (Vector2?)Projectile.position, null);

        Projectile.velocity *= 0f;
        Projectile.Center += oldVelocity;
        Projectile.timeLeft = 300;
        if (!HitTile)
            HitTile = true;
        return false;
    }
}