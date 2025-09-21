using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class GlassPiercer : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DuneProwlerSniper);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 52;
    }

    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.damage = 0;
        NPC.width = 40;
        NPC.height = 56;
        NPC.defense = 4;
        NPC.lifeMax = NPC.downedMoonlord ? 1150 : 875;
        NPC.knockBackResist = 0.1f;
        NPC.value = Item.buyPrice(0, 0, 20, 0);
        NPC.HitSound = SoundID.NPCHit2;
        NPC.DeathSound = SoundID.NPCDeath2;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<DuneProwlerSniperBanner>();
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,

                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
            ]);

    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.IsABestiaryIconDummy)
        {
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter > 6.0)
            {
                NPC.frame.Y = NPC.frame.Y + frameHeight;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= frameHeight * 13)
            {
                NPC.frame.Y = frameHeight;
            }
            return;
        }
        NPC.frameCounter += Math.Abs(NPC.velocity.X);
        if (NPC.frameCounter > 6.0)
        {
            NPC.frameCounter = 0.0;
            NPC.frame.Y = NPC.frame.Y + frameHeight;
        }
        if (NPC.velocity.Y == 0f)
        {
            if (NPC.direction == 1)
            {
                NPC.spriteDirection = 1;
            }
            if (NPC.direction == -1)
            {
                NPC.spriteDirection = -1;
            }
            if (NPC.velocity.X == 0f)
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = 0;
                return;
            }
            if (NPC.frame.Y < frameHeight)
            {
                NPC.frame.Y = frameHeight;
            }
            if (NPC.frame.Y > frameHeight * 13)
            {
                NPC.frame.Y = frameHeight;
            }
        }
        else
        {
            NPC.frameCounter = 0.0;
            NPC.frame.Y = 0;
        }
    }

    public static readonly float StuckJumpPower = -5f;
    public static readonly int RunTime = SecondsToFrames(2.5f);
    public ref float IdleTimer => ref NPC.ai[0];
    public ref float DoorInteractionTimer => ref NPC.ai[1];
    public ref float DoorAttemptTimer => ref NPC.ai[2];
    public ref float StuckTimer => ref NPC.ai[3];
    public bool Running
    {
        get => NPC.AdditionsInfo().ExtraAI[0] == 1;
        set => NPC.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public ref float RunningTimer => ref NPC.AdditionsInfo().ExtraAI[1];

    public override void AI()
    {
        if (NPC.localAI[0] == 0)
        {
            NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlassFocusedSniper>(), NPC.damage, 2f, NPC.whoAmI);
            Running = false;
            NPC.localAI[0] = 1;
            NPC.netUpdate = true;
        }

        NPC.TargetClosest(true);
        Player target = Main.player[NPC.target];
        float maxSpeed = .5f;
        if (target.Distance(NPC.Center) < 90f && !Running)
        {
            Running = true;
            NPC.netUpdate = true;
        }
        if (Running)
        {
            if (NPC.alpha > 0)
                NPC.alpha -= 4;
            RunningTimer++;
            NPC.direction *= -1;
            maxSpeed = 6f;
            if (RunningTimer >= RunTime)
            {
                Running = false;
                RunningTimer = 0;
                NPC.netUpdate = true;
            }
        }
        else
        {
            int maxAlpha = 150;
            if (NPC.alpha < maxAlpha)
            {
                ParticleRegistry.SpawnMistParticle(NPC.RotHitbox().RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(1f, 2f),
                    Main.rand.NextFloat(.3f, .7f), Color.SaddleBrown, Color.Transparent, Main.rand.NextFloat(110f, 160f));
                NPC.alpha += 2;
            }
        }

        // Initial directionY adjustment if NPC is exactly at player's feet level
        if (target.position.Y + (float)target.height == NPC.position.Y + (float)NPC.height)
        {
            NPC.directionY = -1;
        }

        bool forceJump = false;
        Rectangle playerHitbox;
        bool onGround = false;
        bool wasVelocityXZero = false;
        if (NPC.velocity.X == 0f)
        {
            wasVelocityXZero = true;
        }
        if (NPC.justHit)
        {
            wasVelocityXZero = false;
        }

        int stuckTimerMax = 60; // maximum before considering stuck
        bool shouldTurnAround = false;
        bool canOpenDoors = true;

        bool isSpecialTypeForStuckLogic = false;

        bool useStuckDetection = true;

        if (!isSpecialTypeForStuckLogic && useStuckDetection)
        {
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
            {
                shouldTurnAround = true;
            }
            if (NPC.position.X == NPC.oldPosition.X || StuckTimer >= (float)stuckTimerMax || shouldTurnAround)
            {
                StuckTimer += 1f;
            }
            else if ((double)Math.Abs(NPC.velocity.X) > 0.9 && StuckTimer > 0f)
            {
                StuckTimer -= 1f;
            }
            if (StuckTimer > (float)(stuckTimerMax * 10))
            {
                StuckTimer = 0f;
            }
            if (NPC.justHit)
            {
                StuckTimer = 0f;
            }
            if (StuckTimer == (float)stuckTimerMax)
            {
                NPC.netUpdate = true;
            }
            playerHitbox = target.Hitbox;
            if (playerHitbox.Intersects(NPC.Hitbox))
            {
                StuckTimer = 0f;
            }
        }

        // Check if not stuck and not discouraged to pursue
        if (StuckTimer < (float)stuckTimerMax && NPC.DespawnEncouragement_AIStyle3_Fighters_NotDiscouraged(NPC.type, NPC.position, NPC))
        {
            NPC.TargetClosest();
            if (NPC.directionY > 0 && target.Center.Y <= NPC.Bottom.Y)
            {
                NPC.directionY = -1;
            }
        }

        else if (!(DoorAttemptTimer > 0f) || !NPC.DespawnEncouragement_AIStyle3_Fighters_CanBeBusyWithAction(NPC.type))
        {
            if (NPC.velocity.X == 0f)
            {
                if (NPC.velocity.Y == 0f)
                {
                    IdleTimer += 1f;
                    if (IdleTimer >= 2f && MathF.Abs(NPC.velocity.X) < .4f)
                    {
                        NPC.direction *= -1;
                        NPC.spriteDirection = NPC.direction;
                        IdleTimer = 0f;
                    }
                }
            }
            else
            {
                IdleTimer = 0f;
            }
            if (NPC.direction == 0)
            {
                NPC.direction = 1;
            }
        }

        if (NPC.velocity.X < -maxSpeed || NPC.velocity.X > maxSpeed)
        {
            if (NPC.velocity.Y == 0f)
            {
                NPC.velocity *= 0.8f;
            }
        }
        else if (NPC.velocity.X < maxSpeed && NPC.direction == 1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X < 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X += 0.1f;
            if (NPC.velocity.X > maxSpeed)
            {
                NPC.velocity.X = maxSpeed;
            }
        }
        else if (NPC.velocity.X > -maxSpeed && NPC.direction == -1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X > 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X -= 0.1f;
            if (NPC.velocity.X < -maxSpeed)
            {
                NPC.velocity.X = -maxSpeed;
            }
        }

        if (NPC.velocity.Y == 0f || forceJump)
        {
            int tileYBelow = (int)(NPC.position.Y + (float)NPC.height + 7f) / 16;
            int tileYAbove = (int)(NPC.position.Y - 9f) / 16;
            int tileXLeft = (int)NPC.position.X / 16;
            int tileXRight = (int)(NPC.position.X + (float)NPC.width) / 16;
            int tileXCenterLeft = (int)(NPC.position.X + 8f) / 16;
            int tileXCenterRight = (int)(NPC.position.X + (float)NPC.width - 8f) / 16;
            bool missingTilesBelow = false;
            for (int tileX = tileXCenterLeft; tileX <= tileXCenterRight; tileX++)
            {
                if (tileX >= tileXLeft && tileX <= tileXRight && Main.tile[tileX, tileYBelow] == null)
                {
                    missingTilesBelow = true;
                    continue;
                }
                if (Main.tile[tileX, tileYAbove] != null && Main.tile[tileX, tileYAbove].HasUnactuatedTile && Main.tileSolid[Main.tile[tileX, tileYAbove].type])
                {
                    onGround = false;
                    break;
                }
                if (!missingTilesBelow && tileX >= tileXLeft && tileX <= tileXRight && Main.tile[tileX, tileYBelow].HasUnactuatedTile && Main.tileSolid[Main.tile[tileX, tileYBelow].type])
                {
                    onGround = true;
                }
            }
            if (!onGround && NPC.velocity.Y < 0f)
            {
                NPC.velocity.Y = 0f;
            }
            if (missingTilesBelow)
            {
                return;
            }
        }

        if (NPC.velocity.Y >= 0f && (NPC.type != 580 || NPC.directionY != 1))
        {
            int directionOffset = 0; // offset based on velocity direction
            if (NPC.velocity.X < 0f)
            {
                directionOffset = -1;
            }
            if (NPC.velocity.X > 0f)
            {
                directionOffset = 1;
            }
            Vector2 projectedPosition = NPC.position;
            projectedPosition.X += NPC.velocity.X;
            int tileXCheck = (int)((projectedPosition.X + (float)(NPC.width / 2) + (float)((NPC.width / 2 + 1) * directionOffset)) / 16f);
            int tileYCheck = (int)((projectedPosition.Y + (float)NPC.height - 1f) / 16f);
            if (WorldGen.InWorld(tileXCheck, tileYCheck, 4))
            {
                // absolutely hideous but it works
                if ((float)(tileXCheck * 16) < projectedPosition.X + (float)NPC.width && (float)(tileXCheck * 16 + 16) > projectedPosition.X
                    && ((Main.tile[tileXCheck, tileYCheck].HasUnactuatedTile && !Main.tile[tileXCheck, tileYCheck].TopSlope
                    && !Main.tile[tileXCheck, tileYCheck - 1].TopSlope && Main.tileSolid[Main.tile[tileXCheck, tileYCheck].type]
                    && !Main.tileSolidTop[Main.tile[tileXCheck, tileYCheck].type]) || (Main.tile[tileXCheck, tileYCheck - 1].IsHalfBlock
                    && Main.tile[tileXCheck, tileYCheck - 1].HasUnactuatedTile)) && (!Main.tile[tileXCheck, tileYCheck - 1].HasUnactuatedTile
                    || !Main.tileSolid[Main.tile[tileXCheck, tileYCheck - 1].type] || Main.tileSolidTop[Main.tile[tileXCheck, tileYCheck - 1].type]
                    || (Main.tile[tileXCheck, tileYCheck - 1].IsHalfBlock && (!Main.tile[tileXCheck, tileYCheck - 4].HasUnactuatedTile
                    || !Main.tileSolid[Main.tile[tileXCheck, tileYCheck - 4].type] || Main.tileSolidTop[Main.tile[tileXCheck, tileYCheck - 4].type])))
                    && (!Main.tile[tileXCheck, tileYCheck - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileXCheck, tileYCheck - 2].type]
                    || Main.tileSolidTop[Main.tile[tileXCheck, tileYCheck - 2].type]) && (!Main.tile[tileXCheck, tileYCheck - 3].HasUnactuatedTile
                    || !Main.tileSolid[Main.tile[tileXCheck, tileYCheck - 3].type] || Main.tileSolidTop[Main.tile[tileXCheck, tileYCheck - 3].type])
                    && (!Main.tile[tileXCheck - directionOffset, tileYCheck - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileXCheck - directionOffset, tileYCheck - 3].type]))
                {
                    float groundY = tileYCheck * 16;
                    if (Main.tile[tileXCheck, tileYCheck].IsHalfBlock)
                    {
                        groundY += 8f;
                    }
                    if (Main.tile[tileXCheck, tileYCheck - 1].IsHalfBlock)
                    {
                        groundY -= 8f;
                    }
                    if (groundY < projectedPosition.Y + (float)NPC.height)
                    {
                        float heightDifference = projectedPosition.Y + (float)NPC.height - groundY;
                        float maxStepHeight = 16.1f;
                        if (heightDifference <= maxStepHeight)
                        {
                            NPC.gfxOffY += NPC.position.Y + (float)NPC.height - groundY;
                            NPC.position.Y = groundY - (float)NPC.height;
                            if (heightDifference < 9f)
                            {
                                NPC.stepSpeed = 1f;
                            }
                            else
                            {
                                NPC.stepSpeed = 2f;
                            }
                        }
                    }
                }
            }
        }

        if (onGround)
        {
            int tileXObstacle = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
            int tileYObstacle = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);

            if (Main.tile[tileXObstacle, tileYObstacle - 1].HasUnactuatedTile && (TileLoader.IsClosedDoor(Main.tile[tileXObstacle, tileYObstacle - 1])
                || Main.tile[tileXObstacle, tileYObstacle - 1].type == 388) && canOpenDoors)
            {
                DoorAttemptTimer += 1f;
                StuckTimer = 0f;
                if (DoorAttemptTimer >= 60f)
                {
                    bool canOpenDuringBloodMoon = NPC.type == 3 || NPC.type == 430 || NPC.type == 635;
                    bool randomGraveyardOpen = target.ZoneGraveyard && Main.rand.Next(60) == 0;
                    if ((!Main.bloodMoon || Main.getGoodWorld) && !randomGraveyardOpen && canOpenDuringBloodMoon)
                    {
                        DoorInteractionTimer = 0f;
                    }
                    NPC.velocity.X = 0.5f * (float)(-NPC.direction);
                    int doorOpenIncrement = 5;
                    if (Main.tile[tileXObstacle, tileYObstacle - 1].type == 388)
                    {
                        doorOpenIncrement = 2;
                    }
                    DoorInteractionTimer += doorOpenIncrement;

                    DoorAttemptTimer = 0f;
                    bool canDestroyDoor = false;
                    if (DoorInteractionTimer >= 10f)
                    {
                        canDestroyDoor = true;
                        DoorInteractionTimer = 10f;
                    }

                    WorldGen.KillTile(tileXObstacle, tileYObstacle - 1, fail: true);
                    if ((this.RunServer() || !canDestroyDoor) && canDestroyDoor && this.RunServer())
                    {
                        if (TileLoader.IsClosedDoor(Main.tile[tileXObstacle, tileYObstacle - 1]))
                        {
                            bool opened = WorldGen.OpenDoor(tileXObstacle, tileYObstacle - 1, NPC.direction);
                            if (!opened)
                            {
                                StuckTimer = stuckTimerMax;
                                NPC.netUpdate = true;
                            }
                            if (this.RunClient() && opened)
                            {
                                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, tileXObstacle, tileYObstacle - 1, NPC.direction);
                            }
                        }
                        if (Main.tile[tileXObstacle, tileYObstacle - 1].type == 388)
                        {
                            bool opened = WorldGen.ShiftTallGate(tileXObstacle, tileYObstacle - 1, closing: false);
                            if (!opened)
                            {
                                StuckTimer = stuckTimerMax;
                                NPC.netUpdate = true;
                            }
                            if (this.RunClient() && opened)
                            {
                                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 4, tileXObstacle, tileYObstacle - 1);
                            }
                        }
                    }
                }
            }
            else
            {
                int spriteDirectionAdjusted = NPC.spriteDirection;
                if ((NPC.velocity.X < 0f && spriteDirectionAdjusted == -1) || (NPC.velocity.X > 0f && spriteDirectionAdjusted == 1))
                {
                    if (NPC.height >= 32 && Main.tile[tileXObstacle, tileYObstacle - 2].HasUnactuatedTile && Main.tileSolid[Main.tile[tileXObstacle, tileYObstacle - 2].type])
                    {
                        if (Main.tile[tileXObstacle, tileYObstacle - 3].HasUnactuatedTile && Main.tileSolid[Main.tile[tileXObstacle, tileYObstacle - 3].type])
                        {
                            NPC.velocity.Y = -8f;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            NPC.velocity.Y = -7f;
                            NPC.netUpdate = true;
                        }
                    }
                    else if (Main.tile[tileXObstacle, tileYObstacle - 1].HasUnactuatedTile && Main.tileSolid[Main.tile[tileXObstacle, tileYObstacle - 1].type])
                    {
                        NPC.velocity.Y = -6f;
                        NPC.netUpdate = true;
                    }
                    else if (NPC.position.Y + (float)NPC.height - (float)(tileYObstacle * 16) > 20f && Main.tile[tileXObstacle, tileYObstacle].HasUnactuatedTile
                        && !Main.tile[tileXObstacle, tileYObstacle].TopSlope && Main.tileSolid[Main.tile[tileXObstacle, tileYObstacle].type])
                    {
                        NPC.velocity.Y = -5f;
                        NPC.netUpdate = true;
                    }
                    else if (NPC.directionY < 0 && NPC.type != 67 && (!Main.tile[tileXObstacle, tileYObstacle + 1].HasUnactuatedTile
                        || !Main.tileSolid[Main.tile[tileXObstacle, tileYObstacle + 1].type]) && (!Main.tile[tileXObstacle + NPC.direction, tileYObstacle + 1].HasUnactuatedTile
                        || !Main.tileSolid[Main.tile[tileXObstacle + NPC.direction, tileYObstacle + 1].type]))
                    {
                        NPC.velocity.Y = -8f;
                        NPC.velocity.X *= 1.5f;
                        NPC.netUpdate = true;
                    }
                    else if (canOpenDoors)
                    {
                        DoorInteractionTimer = 0f;
                        DoorAttemptTimer = 0f;
                    }

                    if (NPC.velocity.Y == 0f && wasVelocityXZero && StuckTimer == 1f)
                    {
                        NPC.velocity.Y = StuckJumpPower;
                    }

                    if (NPC.velocity.Y == 0f && (Main.expertMode || NPC.type == 586) && target.Bottom.Y < NPC.Top.Y
                        && Math.Abs(NPC.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(NPC, target))
                    {
                        if (NPC.velocity.Y == 0f)
                        {
                            int maxJumpHeightTiles = 6;
                            if (target.Bottom.Y > NPC.Top.Y - (float)(maxJumpHeightTiles * 16))
                            {
                                NPC.velocity.Y = -7.9f;
                            }
                            else
                            {
                                int tileXCenter = (int)(NPC.Center.X / 16f);
                                int tileYBottom = (int)(NPC.Bottom.Y / 16f) - 1;
                                for (int tileY = tileYBottom; tileY > tileYBottom - maxJumpHeightTiles; tileY--)
                                {
                                    if (Main.tile[tileXCenter, tileY].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[tileXCenter, tileY].type])
                                    {
                                        NPC.velocity.Y = -7.9f;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (canOpenDoors)
        {
            DoorInteractionTimer = 0f;
            DoorAttemptTimer = 0f;
        }
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (Main.hardMode && !spawnInfo.PlayerInTown && spawnInfo.Player.ZoneDesert && !spawnInfo.Invasion)
            return .04f;
        return 0f;
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        for (int i = 0; i < 3; i++)
        {
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection, -1f, 0, default(Color), 1f);
        }
        if (NPC.life <= 0)
        {
            for (int j = 0; j < 15; j++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection, -2f, 0, default(Color), 1f);
            }
        }
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FulguriteInAJar>(), 1, 1, 3));
    }
}

public class GlassFocusedSniper : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlassFocusedSniper);
    public override void SetDefaults()
    {
        Projectile.Size = new(72, 24);
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public int NPCIndex
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int Time
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public static int ShotDamage => FixDamageFromDifficulty(DifficultyBasedValue(120, 200, 280));

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        NPC owner = Main.npc?[NPCIndex] ?? null;
        if (owner == null || !owner.active || owner.type != ModContent.NPCType<GlassPiercer>())
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 3;

        Player target = Main.player?[owner.target] ?? null;

        if (target != null && target.active && !target.dead && !target.ghost && owner.As<GlassPiercer>().Running != true)
        {
            float turnAmt = .09f;
            if (Main.expertMode)
                turnAmt = .12f;
            if (Main.masterMode)
                turnAmt = .14f;
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center + target.velocity.ClampLength(0f, 50f) * 10f), turnAmt);
            Time = (Time + 1) % 60;
        }
        else
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Vector2.UnitX * owner.direction, .1f);
            Time = 0;
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = owner.RotHitbox().Center + PolarVector(25f, Projectile.rotation) + PolarVector(3f, Projectile.rotation + MathHelper.PiOver2);
        if (Time % 60 == 59 && Collision.CanHit(Projectile, target))
        {
            Vector2 tip = Projectile.RotHitbox().Right;

            if (this.RunServer())
            {
                Projectile.NewProj(tip, Projectile.velocity * 10f, ModContent.ProjectileType<GlassFocusedShot>(), ShotDamage, 2f, Main.myPlayer);
                Projectile.NewProj(Projectile.Center, -Projectile.velocity.RotatedBy(.4f * Projectile.direction) * Main.rand.NextFloat(2f, 5f), ModContent.ProjectileType<GlassShell>(), 0, 0f, Main.myPlayer);
            }
            for (int i = 0; i < 12; i++)
            {
                ParticleRegistry.SpawnSparkParticle(tip, Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 8f), 40, Main.rand.NextFloat(.3f, .5f), Color.OrangeRed);
                ParticleRegistry.SpawnGlowParticle(tip, Vector2.Zero, 12, 30f, Color.OrangeRed, 1.1f);
            }
            this.Sync();
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;
        SpriteEffects fx = Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, lightColor, Projectile.rotation, orig, 1f, fx);
        return false;
    }
}

public class GlassShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EmptyRound);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = ProjAIStyleID.GroundProjectile;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 700;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public bool TouchedGrass
    {
        get => Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override void AI()
    {
        if (!TouchedGrass)
        {
            Projectile.rotation += 0.5f * Projectile.direction;
        }
        Projectile.velocity.Y -= 0.055f;
        Projectile.velocity.X *= 0.992f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!TouchedGrass)
        {
            TouchedGrass = true;
            this.Sync();
        }
        Projectile.velocity *= 0.98f;
        return false;
    }
}

public class GlassFocusedShot : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.Size = new(6);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.MaxUpdates = 7;
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 20);

        points.Update(Projectile.Center);
    }

    private float WidthFunction(float c)
    {
        return Projectile.width * MathHelper.SmoothStep(1f, 0f, c);
    }

    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.OrangeRed * GetLerpBump(0f, .1f, .8f, .27f, c.X) * Projectile.Opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail._disposed || points == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 1);
            trail.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
