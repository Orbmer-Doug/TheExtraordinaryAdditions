using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class WitheredShredderShield : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WitheredShredder);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public NPC Target => NPCTargeting.MinionHoming(new(Projectile.Center, 1000, false, true), Owner);

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
        Projectile.minionSlots = 2f;
        Projectile.penetrate = -1;
        Projectile.width = 78;
        Projectile.height = 80;
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
    public ref float ShredTimer => ref Projectile.Additions().ExtraAI[1];
    public ref float AltRotation => ref Projectile.Additions().ExtraAI[2];

    public enum CurrentState
    {
        Hover,
        Charge,
    }

    public enum SubState
    {
        Position,
        Reelback,
        Ram,
        Shred,
    }

    public SubState subState
    {
        get => (SubState)Projectile.ai[1];
        set => Projectile.ai[1] = (float)value;
    }

    public CurrentState State
    {
        get => (CurrentState)Projectile.ai[2];
        set => Projectile.ai[2] = (float)value;
    }

    public bool HasHitTarget
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

    public ref float AltCounter => ref Projectile.Additions().ExtraAI[2];

    public bool Init
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }

    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[4] == 1f;
        set => Projectile.Additions().ExtraAI[4] = value.ToInt();
    }

    private bool CheckActive()
    {
        if (Owner.dead || !Owner.active)
        {
            Owner.ClearBuff(ModContent.BuffType<FlockOfRazorShields>());

            return false;
        }

        if (Owner.HasBuff(ModContent.BuffType<FlockOfRazorShields>()))
        {
            Projectile.timeLeft = 2;
        }

        return true;
    }

    public LoopedSound SawSlot;
    public override void AI()
    {
        after ??= new(8, () => Projectile.Center);

        if (!Init)
        {
            Projectile.owner = Owner.whoAmI;
            HasHitTarget = false;
            Projectile.damage = Owner.HeldItem.damage;
            Projectile.netUpdate = true;
            State = CurrentState.Hover;
            subState = SubState.Position;
            Init = true;
        }

        if (!CheckActive())
            Projectile.Kill();

        if (Target != null)
        {
            State = CurrentState.Charge;
            Charge();
        }
        else
        {
            State = CurrentState.Hover;
            subState = SubState.Position;
            Hover();
        }

        SawSlot ??= new LoopedSound(AssetRegistry.GetSound(AdditionsSound.chainsawThrown) with { MaxInstances = 40 },
            () => new ProjectileAudioTracker(Projectile).IsActiveAndInGame() && State == CurrentState.Charge && subState == SubState.Shred);
        SawSlot.Update(() => Projectile.Center, () => Utils.Remap(ShredTimer, SecondsToFrames(4.3f), SecondsToFrames(5), .3f, 0f), () => 0f);

        bool ramming = subState == SubState.Ram && HasHitTarget == false;
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0,
            (byte)(ramming ? 255 : 40), ramming ? 0 : 2, ramming ? 0f : 4f));

        Projectile.VelocityBasedRotation();
        Projectile.ProjAntiClump(.19f);
    }

    private void Hover()
    {
        // Keep everything at zero assuming there is no enemy to attack
        Timer = 0f;
        HasHitTarget = false;

        Vector2 origin = Owner.Center;
        origin.Y -= 58f; // Go up from the player

        origin.X += (10 + Projectile.minionPos * 30) * -Owner.direction;

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
    }

    private void Charge()
    {
        // Make it not fly into oblivion if it didn't manage to hit something
        float dist = Projectile.Distance(Owner.Center);

        // Run back if too far
        if (dist > 1750f)
        {
            Projectile.velocity = Projectile.Center.SafeDirectionTo(Owner.Center) * 40f;
        }

        // Variables
        float speed = 40f;
        float distance = 300f;
        float rot = MathHelper.Pi;
        Vector2 spawnOffset = Vector2.UnitY.RotatedBy(MathHelper.Lerp(-rot, rot, Projectile.whoAmI % 16f / 16f)) * distance;
        if (Projectile.whoAmI * 113 % 2 == 1)
            spawnOffset *= -1f;

        Vector2 destination = Target.Center + spawnOffset;

        Vector2 targetDestination = Utility.GetHomingVelocity(Projectile.position, Target.position, Target.velocity, speed);//Target.Center + Target.velocity * 2f;
        switch (subState)
        {
            case SubState.Position:
                // Hover to the target
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(destination) * speed, 0.2f).RotatedBy(.05f);
                if (Projectile.WithinRange(destination, Projectile.velocity.Length() * 1.35f))
                {
                    // Reel back
                    Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * -11f;
                    subState = SubState.Reelback;
                    SyncVariables();
                    Projectile.netUpdate = true;
                }
                break;
            case SubState.Reelback:
                // Slow down
                Projectile.velocity *= 0.975f;
                Timer++;

                if (Timer >= ReelBackTime)
                {
                    // Accelerate
                    Projectile.velocity = targetDestination;
                    subState = SubState.Ram;
                    SyncVariables();
                    Timer = 0f;
                    Projectile.netUpdate = true;
                }
                break;
            case SubState.Ram:

                // Make pretty trail sparks
                if (!HasHitTarget)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 pos = Projectile.Center + Utils.ToRotationVector2(i * MathHelper.PiOver2 + Projectile.rotation) * 10f;
                        ParticleRegistry.SpawnSparkParticle(pos, -Projectile.velocity * Main.rand.NextFloat(.1f, .4f), 20, .3f, Color.AntiqueWhite);
                    }
                }
                if (HasHitTarget == true)
                    AltCounter++;

                // Halt to stop
                if (HasHitTarget == true && AltCounter < 40f)
                {
                    Projectile.velocity *= .9f;
                }

                // Go shred if ready
                if (HasHitTarget == true && AltCounter > 40f)
                {
                    subState = SubState.Shred;
                    SyncVariables();
                }

                // Otherwise repeat
                else if (HasHitTarget == false && AltCounter > ReelBackTime * 2f)
                {
                    Timer = 0f;
                    PlayedSound = false;
                    HasHitTarget = false;
                    AltCounter = 0f;
                    subState = SubState.Position;
                    SyncVariables();
                }

                if (PlayedSound == false && !Main.dedServ)
                {
                    SoundID.DD2_WyvernDiveDown.Play(Projectile.Center, 1.2f, -.1f);
                    PlayedSound = true;
                }
                break;
            case SubState.Shred:
                {
                    Shred();
                }
                break;
        }
    }

    public void Shred()
    {
        ShredTimer++;

        if (ShredTimer >= SecondsToFrames(5))
        {
            Timer = 0f;
            HasHitTarget = false;
            AltCounter = 0f;
            PlayedSound = false;
            ShredTimer = 0f;
            subState = SubState.Position;
            SyncVariables();
        }

        if (ShredTimer % 6f == 5f)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;
                Vector2 vel = Main.rand.NextVector2CircularEdge(10, 10);
                int life = Main.rand.Next(30, 80);
                float reduction = Main.rand.NextFloat(.5f, .7f);
                float scale = Main.rand.NextFloat(0.9f, 1.2f);
                Color color = (Main.rand.NextBool(3) ? Color.DarkRed : Color.Crimson) * 0.75f;
                ParticleRegistry.SpawnBloodParticle(pos, vel * reduction, life, scale, color);
            }
        }

        float comp = InverseLerp(0f, 50f, ShredTimer % 50);
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center + GetPointOnLemniscate(comp, -AltRotation, 300f)) * 24f, .6f);
        AltRotation = (AltRotation + .05f) % MathHelper.TwoPi;
    }

    public void SyncVariables()
    {
        Projectile.netUpdate = true;
        if (Projectile.netSpam >= 10)
        {
            Projectile.netSpam = 9;
        }
    }

    public override bool? CanDamage()
    {
        if (subState == SubState.Ram || subState == SubState.Shred)
            return null;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HasHitTarget)
            HasHitTarget = true;

        // Impact particles
        for (int i = 0; i < 40; i++)
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.33f, .66f);

            ParticleRegistry.SpawnSparkleParticle(pos, vel, 30, Main.rand.NextFloat(.2f, .4f), Color.OrangeRed, Color.DarkRed);
            ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, 1.2f), Color.Chocolate);
        }
        AdditionsSound.PlasticHit.Play(Projectile.Center, .6f, -.1f, .2f, 10, Name);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;

        if (subState == SubState.Reelback)
        {
            // Make it glowy right before charging
            float interpol = Convert01To010(Timer / ReelBackTime);
            Projectile.DrawProjectileBackglow(Color.DarkRed, interpol * 10f, 90, 12);
        }

        Color mainColor = subState == SubState.Shred ? Color.DarkRed : lightColor;
        float rotation = Projectile.rotation;

        bool ramming = subState == SubState.Ram && HasHitTarget == false;
        if (ramming || subState == SubState.Shred) 
        {
            Color[] col = ramming ? [lightColor] : [Color.DarkRed, Color.Red];
            after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), col, Projectile.Opacity);
        }

        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(mainColor), rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}
