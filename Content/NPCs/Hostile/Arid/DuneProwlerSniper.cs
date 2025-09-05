using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class DuneProwlerSniper : ModNPC
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

                new FlavorTextBestiaryInfoElement("Stealthy versions of the arid desert clan, these sharpshooters are lethal to anything not behind cover")
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




    private bool run;
    public ref float RunTimer => ref NPC.AdditionsInfo().ExtraAI[4];
    private void Move1()
    {
        Player target = Main.player[NPC.target];

        NPC.spriteDirection = NPC.direction > 0 ? 1 : -1;
        int alphaAmount = 150;
        Tile val2;

        float speedDetect = 6f;
        float speedAdditive = .09f;
        if (run == true)
        {
            RunTimer++;
            if (RunTimer >= 300f)
            {
                RunTimer = 0f;
                run = false;
            }
            if (NPC.alpha > 0)
            {
                NPC.alpha -= alphaAmount / 16;
                if (NPC.alpha < 0)
                {
                    NPC.alpha = 0;
                }
            }
            NPC.chaseable = true;

            int turnAroundDelay = 30;
            bool isRunning = false;
            bool shouldTurnAround = false;
            if (NPC.velocity.Y == 0f && (NPC.velocity.X > 0f && NPC.direction > 0 || NPC.velocity.X < 0f && NPC.direction < 0))
            {
                isRunning = true;
                NPC.ai[3] += 1f;
            }
            if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= turnAroundDelay || isRunning)
            {
                NPC.ai[3] += 1f;
                shouldTurnAround = true;
            }
            else if (NPC.ai[3] > 0f)
            {
                NPC.ai[3] -= 1f;
            }
            if (NPC.ai[3] > turnAroundDelay * 10)
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.justHit)
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.ai[3] == turnAroundDelay)
            {
                NPC.netUpdate = true;
            }
            Vector2 NPCPos = new(NPC.Center.X, NPC.Center.Y);
            float num = Main.player[NPC.target].Center.X - NPCPos.X;
            float yDist = Main.player[NPC.target].Center.Y - NPCPos.Y;
            if ((float)Math.Sqrt(num * num + yDist * yDist) < 200f && !shouldTurnAround)
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.ai[3] < turnAroundDelay)
            {
                NPC.TargetClosest(true);
            }
            else
            {
                if (NPC.velocity.X == 0f)
                {
                    if (NPC.velocity.Y == 0f)
                    {
                        NPC.ai[0] += 1f;
                        if (NPC.ai[0] >= 2f)
                        {
                            NPC.direction = NPC.direction * -1;
                            NPC.spriteDirection = -NPC.direction;
                            NPC.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    NPC.ai[0] = 0f;
                }
                NPC.directionY = -1;
                if (NPC.direction == 0)
                {
                    NPC.direction = 1;
                }
            }
            if (0 == 0 && (NPC.velocity.Y == 0f || NPC.wet || NPC.velocity.X <= 0f && NPC.direction > 0 || NPC.velocity.X >= 0f && NPC.direction < 0))
            {
                if (NPC.velocity.X < 0f - speedDetect || NPC.velocity.X > speedDetect)
                {
                    if (NPC.velocity.Y == 0f)
                    {
                        NPC.velocity = NPC.velocity * 0.8f;
                    }
                }
                else if (NPC.velocity.X < speedDetect && NPC.direction == -1)
                {
                    NPC.velocity.X = NPC.velocity.X + speedAdditive;
                    if (NPC.velocity.X > speedDetect)
                    {
                        NPC.velocity.X = speedDetect;
                    }
                }
                else if (NPC.velocity.X > 0f - speedDetect && NPC.direction == 1)
                {
                    NPC.velocity.X = NPC.velocity.X - speedAdditive;
                    if (NPC.velocity.X < 0f - speedDetect)
                    {
                        NPC.velocity.X = 0f - speedDetect;
                    }
                }
            }
            if (!(NPC.velocity.Y >= 0f))
            {
                return;
            }
            int faceDirection = 0;
            if (NPC.velocity.X < 0f)
            {
                faceDirection = -1;
            }
            if (NPC.velocity.X > 0f)
            {
                faceDirection = 1;
            }
            Vector2 position = NPC.position;
            position.X += NPC.velocity.X;
            int x = (int)((position.X + NPC.width / 2 + (NPC.width / 2 + 1) * faceDirection) / 16f);
            int y = (int)((position.Y + NPC.height - 1f) / 16f);
            if (!(x * 16 < position.X + NPC.width) || !(x * 16 + 16 > position.X))
            {
                return;
            }
            Tile val = Main.tile[x, y];
            if (val.HasUnactuatedTile)
            {
                val = Main.tile[x, y];
                if (!val.TopSlope)
                {
                    val = Main.tile[x, y - 1];
                    if (!val.TopSlope)
                    {
                        bool[] tileSolid = Main.tileSolid;
                        val = Main.tile[x, y];
                        if (tileSolid[val.TileType])
                        {
                            bool[] tileSolidTop = Main.tileSolidTop;
                            val = Main.tile[x, y];
                            if (!tileSolidTop[val.TileType])
                            {
                                goto IL_0530;
                            }
                        }
                    }
                }
            }
            val = Main.tile[x, y - 1];
            if (val.IsHalfBlock)
            {
                val = Main.tile[x, y - 1];
                if (!val.HasUnactuatedTile)
                {
                    return;
                }
                goto IL_0530;
            }
            return;
        IL_0530:
            val = Main.tile[x, y - 1];
            if (val.HasUnactuatedTile)
            {
                bool[] tileSolid2 = Main.tileSolid;
                val = Main.tile[x, y - 1];
                if (tileSolid2[val.TileType])
                {
                    bool[] tileSolidTop2 = Main.tileSolidTop;
                    val = Main.tile[x, y - 1];
                    if (!tileSolidTop2[val.TileType])
                    {
                        val = Main.tile[x, y - 1];
                        if (!val.IsHalfBlock)
                        {
                            return;
                        }
                        val = Main.tile[x, y - 4];
                        if (val.HasUnactuatedTile)
                        {
                            bool[] tileSolid3 = Main.tileSolid;
                            val = Main.tile[x, y - 4];
                            if (tileSolid3[val.TileType])
                            {
                                bool[] tileSolidTop3 = Main.tileSolidTop;
                                val = Main.tile[x, y - 4];
                                if (!tileSolidTop3[val.TileType])
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            val = Main.tile[x, y - 2];
            if (val.HasUnactuatedTile)
            {
                bool[] tileSolid4 = Main.tileSolid;
                val = Main.tile[x, y - 2];
                if (tileSolid4[val.TileType])
                {
                    bool[] tileSolidTop4 = Main.tileSolidTop;
                    val = Main.tile[x, y - 2];
                    if (!tileSolidTop4[val.TileType])
                    {
                        return;
                    }
                }
            }
            val = Main.tile[x, y - 3];
            if (val.HasUnactuatedTile)
            {
                bool[] tileSolid5 = Main.tileSolid;
                val = Main.tile[x, y - 3];
                if (tileSolid5[val.TileType])
                {
                    bool[] tileSolidTop5 = Main.tileSolidTop;
                    val = Main.tile[x, y - 3];
                    if (!tileSolidTop5[val.TileType])
                    {
                        return;
                    }
                }
            }
            val = Main.tile[x - faceDirection, y - 3];
            if (val.HasUnactuatedTile)
            {
                bool[] tileSolid6 = Main.tileSolid;
                val = Main.tile[x - faceDirection, y - 3];
                if (tileSolid6[val.TileType])
                {
                    return;
                }
            }
            float NPCBottom = y * 16;
            val = Main.tile[x, y];
            if (val.IsHalfBlock)
            {
                NPCBottom += 8f;
            }
            val = Main.tile[x, y - 1];
            if (val.IsHalfBlock)
            {
                NPCBottom -= 8f;
            }
            if (!(NPCBottom < position.Y + NPC.height))
            {
                return;
            }
            float percentageTileRisen = position.Y + NPC.height - NPCBottom;
            if (percentageTileRisen <= 16.1f)
            {
                NPC.gfxOffY += NPC.position.Y + NPC.height - NPCBottom;
                NPC.position.Y = NPCBottom - NPC.height;
                if (percentageTileRisen < 9f)
                {
                    NPC.stepSpeed = 1f;
                }
                else
                {
                    NPC.stepSpeed = 2f;
                }
            }
        }
        else if (NPC.ai[2] == 0f)
        {
            NPC.chaseable = false;
            if (NPC.alpha >= 0)
            {
                if (NPC.alpha != alphaAmount)
                {
                    if (NPC.AdditionsInfo().ExtraAI[5]++ % 3f == 0f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ambient_DarkBrown, -1f, -1f, 0, default, 1f);
                        }
                    }
                    NPC.alpha += alphaAmount / 16;
                }
                if (NPC.alpha < 0)
                {
                    NPC.alpha = 0;
                }
                if (NPC.alpha > alphaAmount)
                {
                    NPC.alpha = alphaAmount;
                }
            }

            NPC.TargetClosest(true);
            if (!Main.player[NPC.target].dead)
            {
                Vector2 val = Main.player[NPC.target].Center - NPC.Center;
                if (Vector2.Distance(NPC.Center, target.Center) < 100 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    run = true;
                }
            }
            if (NPC.justHit)
            {
                run = true;
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.velocity.X * -1f;
                NPC.direction = NPC.direction * -1;
            }
            if (NPC.collideY)
            {
                if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = Math.Abs(NPC.velocity.Y) * -1f;
                    NPC.directionY = -1;
                    NPC.ai[0] = -1f;
                }
                else if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y = Math.Abs(NPC.velocity.Y);
                    NPC.directionY = 1;
                    NPC.ai[0] = 1f;
                }
            }
            NPC.velocity.X = NPC.velocity.X + NPC.direction * 0.02f;
            NPC.rotation = NPC.velocity.X * 0.4f;
            if (NPC.velocity.X < -1f || NPC.velocity.X > 1f)
            {
                NPC.velocity.X = NPC.velocity.X * 0.95f;
            }
            if (NPC.ai[0] == -1f)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.01f;
                if (NPC.velocity.Y < -1f)
                {
                    NPC.ai[0] = 1f;
                }
            }
            else
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.01f;
                if (NPC.velocity.Y > 1f)
                {
                    NPC.ai[0] = -1f;
                }
            }
            int num4 = (int)(NPC.position.X + NPC.width / 2) / 16;
            int num5 = (int)(NPC.position.Y + NPC.height / 2) / 16;
            val2 = Main.tile[num4, num5 - 1];
            if (val2.LiquidAmount > 128)
            {
                val2 = Main.tile[num4, num5 + 1];
                if (val2.HasTile)
                {
                    NPC.ai[0] = -1f;
                }
                else
                {
                    val2 = Main.tile[num4, num5 + 2];
                    if (val2.HasTile)
                    {
                        NPC.ai[0] = -1f;
                    }
                }
            }
            else
            {
                NPC.ai[0] = 1f;
            }
            if (NPC.velocity.Y > 1.2 || NPC.velocity.Y < -1.2)
            {
                NPC.velocity.Y = NPC.velocity.Y * 0.99f;
            }
        }



        else if (NPC.ai[2] < 0f)
        {
            if (NPC.alpha > 0)
            {
                NPC.alpha -= alphaAmount / 16;
                if (NPC.alpha < 0)
                {
                    NPC.alpha = 0;
                }
            }
            NPC.ai[2] += 1f;
            if (NPC.ai[2] == 0f)
            {
                NPC.ai[0] = 0f;
                NPC.ai[2] = 1f;
                NPC.velocity.X = NPC.direction * 2;
            }
        }


        else
        {
            if (NPC.ai[2] != 1f)
            {
                return;
            }
            NPC.chaseable = true;
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(true);
            }
            if (NPC.wet || NPC.noTileCollide)
            {
                bool flag14 = false;
                NPC.TargetClosest(false);
                if (Main.player[NPC.target].wet && !Main.player[NPC.target].dead)
                {
                    flag14 = true;
                }
                if (!flag14)
                {
                    if (!Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
                    {
                        NPC.noTileCollide = false;
                    }
                    if (NPC.collideX)
                    {
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC NPC3 = NPC;
                        NPC3.direction = NPC3.direction * -1;
                        NPC.netUpdate = true;
                    }
                    if (NPC.collideY)
                    {
                        NPC.netUpdate = true;
                        if (NPC.velocity.Y > 0f)
                        {
                            NPC.velocity.Y = Math.Abs(NPC.velocity.Y) * -1f;
                            NPC.directionY = -1;
                            NPC.ai[0] = -1f;
                        }
                        else if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = Math.Abs(NPC.velocity.Y);
                            NPC.directionY = 1;
                            NPC.ai[0] = 1f;
                        }
                    }
                }
                if (flag14)
                {
                    if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                    {
                        if (NPC.ai[3] > 0f && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
                        {
                            NPC.ai[3] = 0f;
                            NPC.ai[1] = 0f;
                            NPC.netUpdate = true;
                        }
                    }
                    else if (NPC.ai[3] == 0f)
                    {
                        NPC.ai[1] += 1f;
                    }
                    if (NPC.ai[1] >= 150f)
                    {
                        NPC.ai[3] = 1f;
                        NPC.ai[1] = 0f;
                        NPC.netUpdate = true;
                    }
                    if (NPC.ai[3] == 0f)
                    {
                        NPC.alpha = 0;
                        NPC.noTileCollide = false;
                    }
                    else
                    {
                        NPC.alpha = 150;
                        NPC.noTileCollide = true;
                    }
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + NPC.direction * 0.2f;
                    NPC.velocity.Y = NPC.velocity.Y + NPC.directionY * 0.2f;
                    if (NPC.velocity.X > 9f)
                    {
                        NPC.velocity.X = 9f;
                    }
                    if (NPC.velocity.X < -9f)
                    {
                        NPC.velocity.X = -9f;
                    }
                    if (NPC.velocity.Y > 7f)
                    {
                        NPC.velocity.Y = 7f;
                    }
                    if (NPC.velocity.Y < -7f)
                    {
                        NPC.velocity.Y = -7f;
                    }
                }
                else
                {
                    if (!Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
                    {
                        NPC.noTileCollide = false;
                    }
                    NPC.velocity.X = NPC.velocity.X + NPC.direction * 0.1f;
                    if (NPC.velocity.X < -1f || NPC.velocity.X > 1f)
                    {
                        NPC.velocity.X = NPC.velocity.X * 0.95f;
                    }
                    if (NPC.ai[0] == -1f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y - 0.01f;
                        if (NPC.velocity.Y < -0.3)
                        {
                            NPC.ai[0] = 1f;
                        }
                    }
                    else
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.01f;
                        if (NPC.velocity.Y > 0.3)
                        {
                            NPC.ai[0] = -1f;
                        }
                    }
                }
                int posx = (int)(NPC.position.X + NPC.width / 2) / 16;
                int posy = (int)(NPC.position.Y + NPC.height / 2) / 16;
                val2 = Main.tile[posx, posy - 1];
                if (val2.LiquidAmount > 128)
                {
                    val2 = Main.tile[posx, posy + 1];
                    if (val2.HasTile)
                    {
                        NPC.ai[0] = -1f;
                    }
                    else
                    {
                        val2 = Main.tile[posx, posy + 2];
                        if (val2.HasTile)
                        {
                            NPC.ai[0] = -1f;
                        }
                    }
                }
                if (NPC.velocity.Y > 0.4 || NPC.velocity.Y < -0.4)
                {
                    NPC.velocity.Y = NPC.velocity.Y * 0.95f;
                }
            }




            else
            {
                if (NPC.velocity.Y == 0f)
                {
                    NPC.velocity.X = NPC.velocity.X * 0.94f;
                    if (NPC.velocity.X > -0.2 && NPC.velocity.X < 0.2)
                    {
                        NPC.velocity.X = 0f;
                    }
                }
                NPC.velocity.Y = NPC.velocity.Y + 0.25f;
                if (NPC.velocity.Y > 7f)
                {
                    NPC.velocity.Y = 7f;
                }
                NPC.ai[0] = 1f;
            }


            NPC.rotation = NPC.velocity.Y * NPC.direction * 0.05f;
            if (NPC.rotation < -0.2)
            {
                NPC.rotation = -0.2f;
            }
            if (NPC.rotation > 0.2)
            {
                NPC.rotation = 0.2f;
            }
        }

    }

    public override void AI()
    {
        Move1();
        NPC.AdditionsInfo().ExtraAI[3]++;
        float a = NPC.AdditionsInfo().ExtraAI[3];
        if (a == 1f)
        {
            NPC.velocity.X = 0f;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int turret = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<GlassFocusedSniper>());
                Main.npc[turret].ai[1] = NPC.whoAmI;
                Main.npc[turret].target = NPC.target;
                Main.npc[turret].netUpdate = true;

                NPC.TargetClosest(); // target dammit
                NPC.netUpdate = true;
            }
        }
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (!spawnInfo.PlayerSafe && spawnInfo.Player.ZoneDesert && Main.hardMode && NPC.CountNPCS(ModContent.NPCType<DuneProwlerSniper>()) < 1 && NoInvasion(spawnInfo.Player))
        {
            return .05f;
        }
        return 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
        {

        }
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
