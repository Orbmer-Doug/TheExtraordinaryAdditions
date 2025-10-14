using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;

public class EnchantedShield : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EnchantedShield);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public NPC Target => NPCTargeting.MinionHoming(new(Projectile.Center, 900), Owner);

    public const float ReelBackTime = 50f;
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 1;
        Main.projPet[Projectile.type] = false;

        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 12000;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.minionSlots = 1f;
        Projectile.penetrate = -1;
        Projectile.width = 20;
        Projectile.height = 38;
        Projectile.scale = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.minion = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.netImportant = true;
    }
    public override bool MinionContactDamage() => true;
    public override bool? CanCutTiles() => false;

    public ref float Timer => ref Projectile.ai[0];
    public ref float State => ref Projectile.ai[1];
    public bool HasHitTarget
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float Counter => ref Projectile.AdditionsInfo().ExtraAI[0];
    public bool PlayedSound
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public bool Init
    {
        get => Projectile.AdditionsInfo().ExtraAI[2] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }

    private bool CheckActive()
    {
        if (Owner.dead || !Owner.active)
        {
            Owner.ClearBuff(ModContent.BuffType<FlockOfShields>());
            return false;
        }
        if (Owner.HasBuff(ModContent.BuffType<FlockOfShields>()))
            Projectile.timeLeft = 2;
        return true;
    }

    public override void AI()
    {
        if (!Init)
        {
            Projectile.owner = Owner.whoAmI;
            HasHitTarget = false;
            Projectile.damage = Owner.HeldItem.damage;
            Projectile.netUpdate = true;
            Init = true;
            this.Sync();
        }

        if (!CheckActive())
            Projectile.Kill();

        after ??= new(8, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        if (Target != null)
            Charge();
        else
            Hover();

        Projectile.ProjAntiClump(.19f);
    }

    private void Hover()
    {
        // Keep everything at zero assuming there is no enemy to attack
        State = 0f;
        Timer = 0f;
        HasHitTarget = false;

        Vector2 origin = Owner.Center;
        origin.Y -= 58f; // Go up from the player

        float minionPositionOffsetX = (10 + Projectile.minionPos * 30) * -Owner.direction;
        origin.X += minionPositionOffsetX; // Go behind the player

        if (Projectile.WithinRange(origin, 1200f) && !Projectile.WithinRange(origin, 300f))
        {
            Projectile.velocity = (origin - Projectile.Center) / 30f;
        }
        else if (!Projectile.WithinRange(origin, 160f))
        {
            Projectile.velocity = (Projectile.velocity * 37f + Projectile.SafeDirectionTo(origin) * 17f) / 40f;
        }
        if (!Projectile.WithinRange(origin, 1200f))
        {
            Projectile.position = origin;
            Projectile.velocity *= 0.3f;
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    private void Charge()
    {
        // Make it not fly into oblivion if it didn't manage to hit something
        float dist = Projectile.Distance(Owner.Center);
        if (dist > 1600f)
        {
            State = 0f;
            HasHitTarget = false;
            Projectile.Center = Owner.Center;
            this.Sync();
        }

        // Variables
        float speed = 28f;
        float distance = Target.Size.Length() + 100;
        float rot = .97f;
        rot = MathHelper.Pi;
        Vector2 spawnOffset = Vector2.UnitY.RotatedBy(MathHelper.Lerp(-rot, rot, Projectile.whoAmI % 16f / 16f)) * distance;
        if (Projectile.whoAmI * 113 % 2 == 1)
            spawnOffset *= -1f;

        Vector2 destination = Target.Center + spawnOffset;

        // Set the rotation
        if (HasHitTarget == false)
            Projectile.rotation = Projectile.AngleTo(Target.Center);

        // Hover to the target
        if (State == 0f)
        {
            PlayedSound = false;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(destination) * speed, 0.11f);
            if (Projectile.WithinRange(destination, Projectile.velocity.Length() * 1.35f))
            {
                // Reel back
                Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * -7f;
                State = 1f;
                Projectile.netUpdate = true;
            }
        }
        // Slow down
        if (State == 1f)
        {
            Projectile.velocity *= 0.975f;
            Timer++;

            if (Timer >= ReelBackTime)
            {
                // Accelerate
                Projectile.velocity = Projectile.SafeDirectionTo(Target.Center + Target.velocity * 10f) * speed;
                Projectile.oldPos = new Vector2[Projectile.oldPos.Length];
                State = 2f;
                Timer = 0f;
                Projectile.netUpdate = true;
            }
        }
        if (State == 2f)
        {
            // Make pretty trail sparks
            if (!HasHitTarget)
            {
                ParticleRegistry.SpawnSparkParticle(Projectile.RandAreaInEntity(), -Projectile.velocity * Main.rand.NextFloat(.1f, .4f), 20, .3f, Color.AntiqueWhite);
            }

            // Manage hitting of a target
            if (HasHitTarget == true)
            {
                Projectile.velocity *= .95f;
                Counter++;
                if (Counter > ReelBackTime)
                {
                    HasHitTarget = false;
                    Counter = 0f;
                    State = 0f;
                }
            }

            // Play the sound once
            if (PlayedSound == false)
            {
                SoundID.DD2_WyvernDiveDown.Play(Projectile.Center, 1.2f, 0f, .1f, null, 10, Name);
                PlayedSound = true;
            }
        }
    }

    public override bool? CanDamage()
    {
        if (State == 2f)
            return null;

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HasHitTarget)
            HasHitTarget = true;

        // Impact particles
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.33f, 1f);
            ParticleRegistry.SpawnMistParticle(pos, vel, Main.rand.NextFloat(.4f, .8f), Color.DarkGray, Color.Black, 160);
            Dust.NewDustPerfect(pos, DustID.Stone, vel * Main.rand.NextFloat(.2f, .5f), 0, default, Main.rand.NextFloat(.7f, 1.4f));
        }

        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, Main.rand.Next(14, 18), Main.rand.NextFloat(1f, 2f), Color.White, Color.Gray);

        SoundEngine.PlaySound(SoundID.Tink with { Volume = 1.2f, Pitch = -Main.rand.NextFloat(.34f, .41f) }, pos);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;

        if (State == 1f)
        {
            // Make it glowy right before charging
            float interpol = Convert01To010(Timer / ReelBackTime);
            Projectile.DrawProjectileBackglow(Color.Tan, interpol * 10f, 90, 12);
        }

        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        if (State == 2f && HasHitTarget == false)
            after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor], Projectile.Opacity);
        return false;
    }
}
