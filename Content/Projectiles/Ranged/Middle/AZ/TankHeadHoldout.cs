using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

// this has to be the stupidest thing i have made
public class TankHeadHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TankHeadHoldout);
    public override int AssociatedItemID => ModContent.ItemType<TroubledTank>();
    public override int IntendedProjectileType => ModContent.ProjectileType<TankHeadHoldout>();
    public ref float Counter => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float State => ref Projectile.ai[2];
    public ref float Cooldown => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float Firing => ref Projectile.AdditionsInfo().ExtraAI[1];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 8;
    }

    public override void Defaults()
    {
        Projectile.width = 34;
        Projectile.height = 64;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public static Color GetTeamColor(Player player)
    {
        Color col = Color.Transparent;
        if (player.team == (int)Team.None)
            col = Color.Green;
        if (player.team == (int)Team.Red)
            col = Color.Red;
        if (player.team == (int)Team.Green || Main.netMode == NetmodeID.SinglePlayer)
            col = Color.LimeGreen;
        if (player.team == (int)Team.Blue)
            col = Color.Blue;
        if (player.team == (int)Team.Yellow)
            col = Color.Gold;
        if (player.team == (int)Team.Pink)
            col = Color.Pink;
        return col;
    }

    public const float SightDistance = 900f;
    public const float TimeForLock = 60f;
    public int Recoil;
    public override void SafeAI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Cooldown = SecondsToFrames(5f);
            Projectile.localAI[0] = 1f;
        }
        if (Owner.dead || !Owner.active)
            Projectile.Kill();

        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 40f, Projectile.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.mouseWorld), interpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Owner.SetFrontHandBetter(0, Projectile.velocity.ToRotation());
        Owner.SetBackHandBetter(0, Projectile.velocity.ToRotation());
        Projectile.FacingUp();

        float dist = 30f;
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter) + Projectile.velocity * MathHelper.Clamp(dist - Recoil * 2, 0f, dist);
        Vector2 tipOfGun = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Owner.HeldItem.width * .5f;

        if (Recoil > 0)
            Recoil--;
        if (Cooldown > 0)
            Cooldown--;

        float speed = 5f;

        Vector2 direction = Projectile.SafeDirectionTo(Modded.mouseWorld);
        Vector2 vel = direction * speed;

        Projectile.scale = 1f;

        bool activatingRight = Firing <= 0f && Owner.Additions().SafeMouseRight.JustPressed;
        if (this.RunLocal() && activatingRight)
        {
            State = (State + 1) % 5;
            this.Sync();
        }

        const float GrubState = 1f;
        const float LaserState = 2f;
        const float MaggotState = 3f;
        const float BeeState = 4f;

        if (Firing == 0f)
        {
            if (Cooldown > 0 && Cooldown % 2f == 0f && (State == GrubState || State == BeeState))
                ParticleRegistry.SpawnHeavySmokeParticle(tipOfGun, -Vector2.UnitY.RotatedByRandom(.25f) * Main.rand.NextFloat(4f, 6f),
                    Main.rand.Next(10, 17), Main.rand.NextFloat(.4f, .7f), Color.Gray, .7f, true);

            if (State == GrubState)
            {
                Projectile.scale = 1.1f;
                Projectile.frame = 7;
            }
            if (State == LaserState)
            {
                if (this.RunLocal())
                    Projectile.NewProj(tipOfGun, vel, ModContent.ProjectileType<LaserGuide>(), 0, 0f, Projectile.owner);
                Projectile.frame = 4;
            }
            else if (State == MaggotState)
                Projectile.frame = 5;
            else if (State == BeeState)
                Projectile.frame = 6;
            else
                Projectile.frame = 0;
        }

        bool overheatedGrubber = State == GrubState && Cooldown > 0;
        bool overheatedHiver = State == BeeState && Cooldown > 0;
        if (this.RunLocal() && Firing == 0f && Modded.SafeMouseLeft.Current && !overheatedGrubber && !overheatedHiver)
        {
            Firing = 1f;
            this.Sync();
        }

        if (Firing == 1f)
        {
            Counter++;

            if (Counter == 1f && State != BeeState)
            {
                int type = 0;
                float damage = Projectile.damage;
                if (State == 0f)
                {
                    // The most Spiteful
                    // congnito hazard
                    SoundEngine.PlaySound(SoundID.Item11, tipOfGun);
                    type = ModContent.ProjectileType<Slug>();
                    damage = (int)(Projectile.damage * 1.1f);
                }
                if (State == GrubState)
                {
                    // grubulon
                    // grubmageddon
                    Cooldown = SecondsToFrames(5f);
                    SoundEngine.PlaySound(SoundID.Item11 with { Pitch = -.5f, Volume = 1.3f }, tipOfGun);
                    type = ModContent.ProjectileType<Grub>();
                    damage = Projectile.damage / 15;
                }
                if (State == LaserState)
                {
                    // snipe
                    SoundEngine.PlaySound(SoundID.Item12, tipOfGun);
                    type = ModContent.ProjectileType<Laser>();
                    damage = (int)(Projectile.damage * 1.25f);
                }
                if (State == MaggotState)
                {
                    // fat tip
                    SoundEngine.PlaySound(SoundID.Item20, tipOfGun);
                    type = ModContent.ProjectileType<Maggot>();
                }
                if (this.RunLocal())
                    Projectile.NewProj(tipOfGun, vel, type, (int)damage, 3f, Projectile.owner);
                Recoil = 6;
            }
            else if (State == 4f)
            {
                if (Counter % 2f == 0f)
                {
                    // You have angered the hive!
                    Vector2 veloc = vel.RotatedByRandom(.35f) * Main.rand.NextFloat(.75f, 1.4f);
                    int damage = Projectile.damage / 9;
                    if (this.RunLocal())
                        Projectile.NewProj(tipOfGun, veloc, ModContent.ProjectileType<TheSwarm>(), damage, 0f, Projectile.owner);
                    SoundEngine.PlaySound(SoundID.Item11 with { MaxInstances = 0, Pitch = .15f, Volume = .9f }, tipOfGun);
                }
            }
            if (State == 0f || State == GrubState)
            {
                if (State == GrubState)
                    Projectile.scale = 1.1f;
                Projectile.SetAnimation(4, 7, true);
            }
            if (State == MaggotState)
                Projectile.frame = 0;
            if (State == BeeState)
                Projectile.frame = 6;
            if (Counter > 60f)
            {
                if (State == BeeState)
                    Cooldown = SecondsToFrames(3f);
                Projectile.frameCounter = 0;
                Firing = 0f;
                Counter = 0f;
            }
        }
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = 0;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(GetTeamColor(Owner)), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, effects, 0);
        return false;
    }
}