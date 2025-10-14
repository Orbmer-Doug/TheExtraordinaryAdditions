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

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class DuneProwler : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DuneProwlerAssault);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 52;
    }

    public override void SetDefaults()
    {
        NPC.aiStyle = AIType = -1;
        NPC.width = 40;
        NPC.height = 56;
        NPC.damage = NPC.downedMoonlord ? 84 : 42;
        NPC.lifeMax = NPC.downedMoonlord ? 1650 : 1210;
        NPC.defense = 18;
        NPC.knockBackResist = 0f;
        NPC.value = Item.buyPrice(0, 0, 25, 0);
        NPC.HitSound = SoundID.DD2_OgreRoar;
        NPC.DeathSound = SoundID.NPCDeath2;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<DuneProwlerAssaultBanner>();
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
                NPC.spriteDirection = 1;
            if (NPC.direction == -1)
                NPC.spriteDirection = -1;
            if (NPC.velocity.X == 0f)
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = 0;
                return;
            }
            if (NPC.frame.Y < frameHeight)
                NPC.frame.Y = frameHeight;
            if (NPC.frame.Y > frameHeight * 13)
                NPC.frame.Y = frameHeight;
        }
        else
        {
            NPC.frameCounter = 0.0;
            NPC.frame.Y = 0;
        }
    }

    public static readonly float StuckJumpPower = -10f;
    public ref float IdleTimer => ref NPC.ai[0];
    public ref float DoorInteractionTimer => ref NPC.ai[1];
    public ref float DoorAttemptTimer => ref NPC.ai[2];
    public ref float StuckTimer => ref NPC.ai[3];
    public ref float MaxSpeed => ref NPC.AdditionsInfo().ExtraAI[0];
    public ref float MoveDirectionTimer => ref NPC.AdditionsInfo().ExtraAI[1];
    public override void AI()
    {
        float minSpeedForTurn = .1f;
        float baseMaxSpeed = 2f;
        float maxSpeedIncrease = 6f;
        float speedIncreasePerFrame = 0.04f;

        NPC.TargetClosest(true);
        Player target = Main.player[NPC.target];

        // Update moveDirectionTimer: increment if moving in current direction, reset if direction changes or stopped
        if (NPC.velocity.X * NPC.direction > 0f) // Moving in the same direction as facing
        {
            MoveDirectionTimer += 1f;
        }
        else
        {
            MoveDirectionTimer = 0f; // Reset when direction mismatches or stopped
        }

        // Calculate dynamic MaxSpeed based on moveDirectionTimer
        MaxSpeed = baseMaxSpeed + Math.Min(maxSpeedIncrease, MoveDirectionTimer * speedIncreasePerFrame);

        if (MaxSpeed > 4f && NPC.velocity.Y == 0)
            Dust.NewDustPerfect(Vector2.Lerp(NPC.RotHitbox().BottomLeft, NPC.RotHitbox().BottomRight, Main.rand.NextFloat()), DustID.Cloud, -NPC.velocity.RotatedByRandom(.1f) * .2f, 50, default, 1.5f);

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

        int stuckTimerMax = 60;
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
            if (NPC.shimmerTransparency < 1f)
            {
                // Random sound plays, check with something like Main.rand.Next(1000) == 0)
            }
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
                    if (IdleTimer >= 2f && Math.Abs(NPC.velocity.X) <= minSpeedForTurn)
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

        if (NPC.velocity.X < -MaxSpeed || NPC.velocity.X > MaxSpeed)
        {
            if (NPC.velocity.Y == 0f)
            {
                NPC.velocity *= 0.8f;
            }
        }
        else if (NPC.velocity.X < MaxSpeed && NPC.direction == 1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X < 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X += 0.1f;
            if (NPC.velocity.X > MaxSpeed)
            {
                NPC.velocity.X = MaxSpeed;
            }
        }
        else if (NPC.velocity.X > -MaxSpeed && NPC.direction == -1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X > 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X -= 0.1f;
            if (NPC.velocity.X < -MaxSpeed)
            {
                NPC.velocity.X = -MaxSpeed;
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
            int directionOffset = 0;
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

                    if (NPC.velocity.Y == 0f && target.Bottom.Y < NPC.Top.Y
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

        NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * .02f, .1f);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
        {
            if (NPC.velocity.X > 7f)
                ParticleRegistry.SpawnPulseRingParticle(NPC.direction == 1 ? NPC.Right : NPC.Left, Vector2.Zero, 30, 0f, new(.5f, 1f), 0f, 60f, Color.Gray);
            MoveDirectionTimer = 0f;
            NPC.velocity.X = 0f;
            NPC.netUpdate = true;
        }
    }

    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.Knockback += 2f * InverseLerp(2f, 8f, NPC.velocity.X);
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (Main.hardMode && !spawnInfo.PlayerInTown && spawnInfo.Player.ZoneDesert && !spawnInfo.Invasion)
            return .05f;
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
            for (int j = 0; j < 25; j++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection, -2f, 0, default(Color), 1f);
            }
        }
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EmblazenedEmber>(), 1, 1, 4));
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        return true;
    }
}


/* Below is a much more cleared up version for vanilla's figher ai. This is useful for making enemies that need to do specific logic but want to keep to vanilla similarities.
    public static readonly float StuckJumpPower = -5f;
    public static readonly float MaxSpeed = 4f;
    public ref float IdleTimer => ref NPC.ai[0];
    public ref float DoorInteractionTimer => ref NPC.ai[1];
    public ref float DoorAttemptTimer => ref NPC.ai[2];
    public ref float StuckTimer => ref NPC.ai[3];
    public override void AI()
    {
        NPC.TargetClosest(true);
        Player target = Main.player[NPC.target];

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
            if (NPC.shimmerTransparency < 1f)
            {
                // Random sound plays, check with something like Main.rand.Next(1000) == 0)
            }
            NPC.TargetClosest();
            if (NPC.directionY > 0 && target.Center.Y <= NPC.Bottom.Y)
            {
                NPC.directionY = -1;
            }
        }

        else if (!(DoorAttemptTimer > 0f) || !NPC.DespawnEncouragement_AIStyle3_Fighters_CanBeBusyWithAction(NPC.type))
        {
            if (Main.IsItDay() && (double)(NPC.position.Y / 16f) < Main.worldSurface && NPC.type != 624 && NPC.type != 631)
            {
                NPC.EncourageDespawn(10);
            }
            if (NPC.velocity.X == 0f)
            {
                if (NPC.velocity.Y == 0f)
                {
                    IdleTimer += 1f;
                    if (IdleTimer >= 2f)
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
        

        if (NPC.velocity.X < -MaxSpeed || NPC.velocity.X > MaxSpeed)
        {
            if (NPC.velocity.Y == 0f)
            {
                NPC.velocity *= 0.8f;
            }
        }
        else if (NPC.velocity.X < MaxSpeed && NPC.direction == 1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X < 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X += 0.1f;
            if (NPC.velocity.X > MaxSpeed)
            {
                NPC.velocity.X = MaxSpeed;
            }
        }
        else if (NPC.velocity.X > -MaxSpeed && NPC.direction == -1)
        {
            if (NPC.velocity.Y == 0f && NPC.velocity.X > 0f)
            {
                NPC.velocity.X *= 0.8f;
            }
            NPC.velocity.X -= 0.1f;
            if (NPC.velocity.X < -MaxSpeed)
            {
                NPC.velocity.X = -MaxSpeed;
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

            // Adjust tileXObstacle for specific types that check further ahead - comment out for general
            // if (NPC.type == 109 || NPC.type == 163 || ... || NPC.type == 582) { tileXObstacle = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)((NPC.width / 2 + 16) * NPC.direction)) / 16f); }

            if (Main.tile[tileXObstacle, tileYObstacle - 1].HasUnactuatedTile && (TileLoader.IsClosedDoor(Main.tile[tileXObstacle, tileYObstacle - 1])
                || Main.tile[tileXObstacle, tileYObstacle - 1].type == 388) && canOpenDoors)
            {
                DoorAttemptTimer += 1f;
                StuckTimer = 0f;
                if (DoorAttemptTimer >= 60f)
                {
                    // Specific types that don't open doors during blood moon or good world - comment for general
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

                    // Expert mode or specific type high jump if player above - keep for general, but can adjust
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
*/