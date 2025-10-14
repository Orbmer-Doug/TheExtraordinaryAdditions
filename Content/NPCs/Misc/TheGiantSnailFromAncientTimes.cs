using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Novelty;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Misc;

public class TheGiantSnailFromAncientTimes : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheGiantSnailFromAncientTimes);
    public override string BossHeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.TheGiantSnailFromAncientTimes);

    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.ShouldBeCountedAsBoss[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.damage = NPC.downedMoonlord ? 150 : 50;
        NPC.width = 218;
        NPC.height = 109;
        NPC.defense = 40;
        NPC.lifeMax = NPC.downedMoonlord ? 26350 : 14050;
        NPC.knockBackResist = 0f;
        NPC.value = Item.buyPrice(1, 10, 5, 50);
        NPC.HitSound = SoundID.DD2_OgreRoar;
        NPC.DeathSound = SoundID.NPCDeath2;
        NPC.rarity = 5;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.netAlways = true;
        if (!Main.dedServ)
        {
            Music = MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.sickest_beat_ever));
        }
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundSnow,
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary")),
                new BossBestiaryInfoElement(),
            ]);
    }

    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (projectile.type == ModContent.ProjectileType<Grub>() && projectile.active && projectile is not null)
        {
            NPC.Kill();
        }
    }

    public ref float Timer => ref NPC.ai[0];
    public override void AI()
    {
        if (NPC.ai[1] == 0f)
        {
            Utility.DisplayText(this.GetLocalizedValue("Awaken"), Color.Red);
            NPC.ai[1] = 1f;
            this.Sync();
        }

        Player target = Main.player[NPC.target];
        if (!target.active || target.dead || Vector2.Distance(target.Center, NPC.Center) > 5600f)
        {
            NPC.TargetClosest(false);
        }

        Timer++;

        float interpolant = InverseLerp(NPC.lifeMax, 0f, NPC.life);
        Vector2 dest = target.Center - Vector2.UnitY * (400f + (float)Math.Sin(Timer * MathHelper.Lerp(.06f, .3f, interpolant / 2)) * 30f);
        if (interpolant >= .5f)
            dest = target.Center + PolarVector(MathF.Sin(Timer * MathHelper.Lerp(.002f, .05f, interpolant / 2 + .5f)) * 600f, Timer * .02f);

        NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(dest) * MathHelper.Lerp(10f, 30f, interpolant), MathHelper.Lerp(.1f, .2f, interpolant));
        if (NPC.Distance(dest) < 20f)
            NPC.velocity += Main.rand.NextVector2CircularEdge(2f, 2f);

        if (Timer % 60f == 59f && Vector2.Distance(NPC.Center, target.Center) < 2000)
        {
            AdditionsSound.PETER.Play(NPC.Center, .6f, 0f, .3f, 0);
            Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            int damage = DifficultyBasedValue(30, 50, 65, 69, 80);
            float speed = 10f;
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, direction.RotatedByRandom(.5f) * speed, ModContent.ProjectileType<ParmaJawn>(), damage, 1f);
            NPC.netUpdate = true;
        }

        NPC.spriteDirection = NPC.velocity.X.NonZeroSign();
    }

    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.KnockbackImmunityEffectiveness *= 0;
        modifiers.Knockback += 1;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (spawnInfo.Player.ZoneSnow && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && NPC.CountNPCS(ModContent.NPCType<TheGiantSnailFromAncientTimes>()) <= 0 && Main.snowMoon)
        {
            return 1f;
        }
        return 0f;
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        SoundID.DD2_WyvernDeath.Play(NPC.Center, 2f);
        for (int i = 0; i < 30; i++)
        {
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection, -1f, 0, default, 1f);
        }

        if (NPC.life <= 0)
        {
            ParticleRegistry.SpawnFlash(NPC.Center, 40, .8f, 1000f);
            ParticleRegistry.SpawnBlurParticle(NPC.Center, 120, 2f, 5000f);
            ParticleRegistry.SpawnChromaticAberration(NPC.Center, 50, 1f, 5000f);
            ScreenShakeSystem.New(new(40f, 3f, 50000f), NPC.Center);
            AdditionsSound.WibtorNUKE.Play(NPC.Center, 1f, 0f, 0f, 1, Name, Terraria.Audio.PauseBehavior.PauseWithGame);
            if (this.RunServer())
                NPC.Shoot(NPC.Center, Vector2.UnitY, ModContent.ProjectileType<BloodBeacon>(), 1000000, 0f);
            for (int j = 0; j < 100; j++)
            {
                ParticleRegistry.SpawnBloomLineParticle(NPC.Center, Main.rand.NextVector2Circular(40f, 40f), Main.rand.Next(30, 80), Main.rand.NextFloat(.8f, 1.6f), Color.Crimson);
                ParticleRegistry.SpawnDetailedBlastParticle(NPC.Center, Vector2.Zero, Vector2.One * Main.rand.NextFloat(2000f, 8000f)
                    * new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat()), Vector2.Zero, 50, Color.Red, RandomRotation(), Color.DarkRed);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection, -20f, 0, default, 5f);
            }
        }
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 250, 500));
        npcLoot.Add(ItemDropRule.Common(ItemID.TurtleShell, 1, 3, 5));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TortoiseShell>(), 1, 11, 63));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Polarity>(), 1, 1, 1));
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (NPC.IsABestiaryIconDummy)
            return true;

        SpriteEffects fx = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        float interpolant = InverseLerp(NPC.lifeMax, 0f, NPC.life);
        if (interpolant > .5f)
            NPC.DrawNPCBackglow(Color.Red, 5f, fx, new(0, 0, NPC.width, NPC.height), 0);
        spriteBatch.DrawBetter(NPC.ThisNPCTexture(), NPC.Center, null, drawColor, NPC.rotation, NPC.ThisNPCTexture().Size() / 2, 1f, fx);
        return false;
    }
}