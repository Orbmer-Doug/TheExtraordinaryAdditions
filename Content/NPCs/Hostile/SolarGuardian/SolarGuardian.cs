
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.SolarGuardian;
public class SolarGuardian : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolarGuardian);
    public enum SolarGuardAIState
    {
        Shooting,
        Dashing
    }

    public bool checkedRotationDir;

    public int rotationDir;

    public float MaxVelocity = 10f;

    public float DistanceFromPlayer = 500f;

    public float AmountOfProjectiles = Main.expertMode ? 6f : 3f;

    public float TimeBetweenProjectiles = Main.expertMode ? 40f : 20f;

    public float TimeBetweenBurst = Main.expertMode ? 180f : 120f;

    public float ProjectileSpeed = 8f;

    public float TimeBeforeDash = Main.expertMode ? 100f : 120f;

    public float TimeDashing = 120f;

    public float DashSpeed = Main.expertMode ? 14f : 12.6f;

    public Player Player => Main.player[NPC.target];

    public SolarGuardAIState CurrentState
    {
        get
        {
            return (SolarGuardAIState)NPC.ai[0];
        }
        set
        {
            NPC.ai[0] = (float)value;
        }
    }

    public ref float RotationIncrease => ref NPC.ai[1];

    public ref float TimerForShooting => ref NPC.ai[2];

    public ref float AITimer => ref NPC.ai[3];

    public bool IsDashing
    {
        get
        {
            if (CurrentState == SolarGuardAIState.Dashing && AITimer > TimeBeforeDash)
            {
                return AITimer <= TimeBeforeDash + TimeDashing;
            }
            return false;
        }
    }

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 17;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
        NPCID.Sets.TrailingMode[NPC.type] = 0;
        NPCID.Sets.TrailCacheLength[NPC.type] = 9;
    }

    public override void SetDefaults()
    {
        NPC.npcSlots = 3f;
        NPC.noGravity = true;
        NPC.damage = Main.expertMode ? 105 : 50;
        NPC.width =
        NPC.height = 162;
        NPC.defense = 12;
        NPC.lifeMax = 8000;
        NPC.knockBackResist = 0f;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.value = Item.buyPrice(0, 6, 25, 0);
        NPC.HitSound = SoundID.NPCHit30;
        NPC.DeathSound = SoundID.NPCDeath18;
        NPC.rarity = 4;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<SolarGuardianBanner>();
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,

                new FlavorTextBestiaryInfoElement("Blazing cores erupted from solar flares on stars given sentience and a mission from a unknown source")
            ]);

    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(rotationDir);
        writer.Write(checkedRotationDir);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        rotationDir = reader.ReadInt32();
        checkedRotationDir = reader.ReadBoolean();
    }

    public override void AI()
    {
        if (NPC.target < 0 || NPC.target == 255 || Player.dead || !Player.active)
        {
            NPC.TargetClosest(true);
        }
        AIMovement(Player);
        float distToTarget = NPC.Distance(Player.Center) + 0.1f;
        NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(Player.Center), IsDashing ? 0.0001f * distToTarget : 0.3f);
        Vector2 center = NPC.Center;
        Color cyan = Color.OrangeRed;
        Lighting.AddLight(center, ((Color)cyan).ToVector3());
        switch (CurrentState)
        {
            case SolarGuardAIState.Shooting:
                State_Shooting(Player);
                break;
            case SolarGuardAIState.Dashing:
                State_Dashing(Player);
                break;
        }
    }

    public void AIMovement(Player player)
    {
        if (!checkedRotationDir)
        {
            rotationDir = Main.rand.NextBool(2).ToDirectionInt();
            checkedRotationDir = true;
            NPC.netUpdate = true;
        }
        Vector2 shootingPos = player.Center + new Vector2(MathF.Cos(RotationIncrease) * rotationDir, MathF.Sin(RotationIncrease) * rotationDir) * DistanceFromPlayer;
        RotationIncrease += CurrentState == SolarGuardAIState.Shooting ? 0.03f : 0.008f;
        NPC.velocity = Vector2.Lerp(NPC.velocity, (shootingPos - NPC.Center).SafeNormalize(Vector2.Zero) * 16f, 0.1f);
        NPC.velocity = Vector2.Clamp(NPC.velocity, new Vector2(0f - MaxVelocity, 0f - MaxVelocity), new Vector2(MaxVelocity, MaxVelocity));
        NPC.netUpdate = true;
    }

    public void State_Shooting(Player player)
    {
        if (NPC.Distance(player.Center) > 1400f)
        {
            return;
        }

        AITimer++;
        if (AITimer >= TimeBetweenBurst)
        {
            if (TimerForShooting % TimeBetweenProjectiles == 0f)
            {
                Vector2 vecToPlayer = NPC.SafeDirectionTo(player.Center);
                Vector2 projVelocity = vecToPlayer * ProjectileSpeed;
                Vector2 lightningSpawnPosition = NPC.Center - Vector2.UnitY.RotatedByRandom(0.2); ;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float shootOffsetAngle = MathHelper.Lerp(-0.9f, 0.9f, i / 4f);
                        Vector2 lightningShootVelocity = NPC.SafeDirectionTo(player.Center).RotatedBy(shootOffsetAngle) * 5f;

                        int damage = 24;
                        NPC.Shoot(lightningSpawnPosition, lightningShootVelocity, ModContent.ProjectileType<Sunray>(), damage, 3f, -1, Main.rand.Next(100), 0f, 0f);
                    }
                    NPC.netUpdate = true;
                }
                NPC.velocity -= vecToPlayer * 5f;
                SoundEngine.PlaySound(SoundID.Item28, (Vector2?)NPC.Center, null);
                NPC.netUpdate = true;
            }
            TimerForShooting++;
            if (TimerForShooting >= TimeBetweenProjectiles * AmountOfProjectiles)
            {
                TimerForShooting = 0f;
                AITimer = 0f;
                CurrentState = SolarGuardAIState.Dashing;
                NPC.netUpdate = true;
            }
        }
        else if (AITimer >= TimeBetweenBurst / 2f && AITimer < TimeBetweenBurst)
        {
            Vector2 randPos = Main.rand.NextVector2CircularEdge(170f, 170f);
            Vector2 pos = NPC.Center + randPos;
            Vector2 vel = NPC.DirectionFrom(NPC.Center + NPC.velocity + randPos) * Main.rand.NextFloat(7f, 10f);

            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, 30, Main.rand.NextFloat(.4f, .7f), Color.OrangeRed, 1f);
        }
    }

    public void State_Dashing(Player player)
    {
        float distToTarget = NPC.Distance(player.Center) + 0.1f;
        AITimer++;
        if (AITimer <= TimeBeforeDash)
        {
            NPC.velocity = Vector2.Lerp(NPC.velocity, -NPC.rotation.ToRotationVector2() * 2f, 0.1f);
            NPC.netUpdate = true;
            return;
        }
        if (AITimer > TimeBeforeDash && AITimer <= TimeBeforeDash + TimeDashing)
        {
            if (AITimer % 2f == 1f)
            {
                Vector2 pos = NPC.RandAreaInEntity();
                Vector2 vel = -NPC.velocity * Main.rand.NextFloat(.3f, .8f);
                float size = Main.rand.NextFloat(.3f, .9f);
                ParticleRegistry.SpawnGlowParticle(pos, vel, 30, size, Color.OrangeRed, 1f, false);
            }
            NPC.velocity = NPC.rotation.ToRotationVector2() * (DashSpeed + 2f / (distToTarget * 0.1f));
            NPC.netUpdate = true;
            return;
        }
        AITimer = 0f;
        checkedRotationDir = false;
        CurrentState = SolarGuardAIState.Shooting;
        NPC.netUpdate = true;
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter += IsDashing ? 0.4f : 0.15f;
        NPC.frameCounter %= Main.npcFrameCount[NPC.type];
        int frame = (int)NPC.frameCounter;
        NPC.frame.Y = frame * frameHeight;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (spawnInfo.Player.ZoneSkyHeight && Main.hardMode)
        {
            return 0.03f;
        }
        return 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
        {
            target.AddBuff(BuffID.Daybreak, 180, true, false);
            target.AddBuff(BuffID.OnFire3, 120, true, false);
        }
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        for (int j = 0; j < 3; j++)
        {
            Dust o = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.OrangeTorch, hit.HitDirection * 2, hit.HitDirection * 2, 0, default(Color), 3f);
            o.noGravity = true;
        }
        if (NPC.life <= 0)
        {
            for (int j = 0; j < 70; j++)
            {
            }
        }
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        if (!IsDashing)
        {
            return false;
        }
        return true;
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WrithingLight>(), 1, 1, 1));
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = TextureAssets.Npc[NPC.type].Value;
        Vector2 position = NPC.Center - screenPos;
        Vector2 origin = NPC.frame.Size() * 0.5f;
        position -= new Vector2(texture.Width, texture.Height / Main.npcFrameCount[NPC.type]) * NPC.scale / 2f;
        position += origin * NPC.scale + new Vector2(0f, NPC.gfxOffY);
        float interpolant = AITimer > TimeBeforeDash && AITimer <= TimeBeforeDash + TimeDashing ? 1f - (AITimer - TimeBeforeDash) / TimeDashing : MathHelper.Clamp(AITimer, 0f, TimeBeforeDash) / TimeBeforeDash;
        float AfterimageFade = MathHelper.Lerp(0f, 1f, interpolant);
        if (CurrentState == SolarGuardAIState.Dashing)
        {
            for (int i = 0; i < NPC.oldPos.Length; i++)
            {
                Color val = new(0.79f, 0.94f, 0.98f);
                Color afterimageDrawColor = val * NPC.Opacity * (1f - i / (float)NPC.oldPos.Length) * AfterimageFade;
                Vector2 afterimageDrawPosition = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos;
                spriteBatch.Draw(texture, afterimageDrawPosition, (Rectangle?)NPC.frame, afterimageDrawColor, NPC.rotation - MathHelper.PiOver2, origin, NPC.scale, 0, 0f);
            }
        }
        spriteBatch.Draw(texture, position, (Rectangle?)NPC.frame, drawColor, NPC.rotation - MathHelper.PiOver2, origin, NPC.scale, 0, 0f);
        return false;
    }
}
