using Microsoft.Xna.Framework;
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
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class DuneProwlerAssault : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DuneProwlerAssault);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 52;
    }

    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.damage = NPC.downedMoonlord ? 84 : 42;
        NPC.width = 40;
        NPC.height = 56;
        NPC.defense = 18;
        NPC.lifeMax = NPC.downedMoonlord ? 1650 : 1210;
        NPC.knockBackResist = 0.1f;
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

                new FlavorTextBestiaryInfoElement("Brutes of the clan that resides within the arid desert that chases foes with specially fasioned rifles")
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

    public override void AI()
    {
        NPC.AdditionsInfo().ExtraAI[3]++;
        float a = NPC.AdditionsInfo().ExtraAI[3];
        if (a == 1f)
        {
            NPC.velocity.X = 0f;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int turret = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<RaggedRifle>());
                Main.npc[turret].ai[1] = NPC.whoAmI;
                Main.npc[turret].target = NPC.target;
                Main.npc[turret].netUpdate = true;
                NPC.TargetClosest(); // target dammit
                NPC.netUpdate = true;
            }
        }

        bool flag26 = false;
        if (NPC.velocity.X == 0f)
        {
            flag26 = true;
        }
        if (NPC.justHit)
        {
            //flag26 = false;
        }
        bool isWalking = false;
        bool flag28 = false;
        int backUpTimer = 40;
        if (NPC.velocity.Y == 0f && (NPC.velocity.X > 0f && NPC.direction < 0 || NPC.velocity.X < 0f && NPC.direction > 0))
        {
            isWalking = true;
        }
        if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= backUpTimer || isWalking)
        {
            NPC.ai[3] += 1f;
        }
        else if ((double)Math.Abs(NPC.velocity.X) > 0.9 && NPC.ai[3] > 0f)
        {
            NPC.ai[3] -= 1f;
        }
        if (NPC.ai[3] > backUpTimer * 10)
        {
            NPC.ai[3] = 0f;
        }
        if (NPC.justHit)
        {
            NPC.ai[3] = 0f;
        }
        if (NPC.ai[3] == backUpTimer)
        {
            NPC.netUpdate = true;
        }
        if (NPC.ai[3] < backUpTimer)
        {
            NPC.TargetClosest(true);
        }
        else if (NPC.ai[2] <= 0f)
        {
            if (NPC.velocity.X == 0f)
            {
                if (NPC.velocity.Y == 0f)
                {
                    NPC.ai[0] += 1f;
                    if (NPC.ai[0] >= 2f)
                    {
                        NPC.direction *= -1;
                        NPC.spriteDirection = NPC.direction;
                        NPC.ai[0] = 0f;
                    }
                }
            }
            else
            {
                NPC.ai[0] = 0f;
            }
            if (NPC.direction == 0)
            {
                NPC.direction = 1;
            }
        }
        float max = 1.75f;
        float added = 0.1f;
        if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) < 2000f)
        {
            float max2 = max;
            float num178 = 6f;
            Vector2 val = Main.player[NPC.target].Center - NPC.Center;
            max = max2 + (num178 - ((Vector2)val).Length() * 0.01f);
        }
        if (NPC.velocity.X < 0f - max || NPC.velocity.X > max)
        {
            if (NPC.velocity.Y == 0f)
            {
                NPC.velocity *= 0.7f;
            }
        }
        else if (NPC.velocity.X < max && NPC.direction == 1)
        {
            NPC.velocity.X += added;
            if (NPC.velocity.X > max)
            {
                NPC.velocity.X = max;
            }
        }
        else if (NPC.velocity.X > 0f - max && NPC.direction == -1)
        {
            NPC.velocity.X = NPC.velocity.X - added;
            if (NPC.velocity.X < 0f - max)
            {
                NPC.velocity.X = 0f - max;
            }
        }
        bool isOnSolidTile = false;
        Tile val2;
        if (NPC.velocity.Y == 0f)
        {
            int maxYTile = (int)(NPC.position.Y + NPC.height + 7f) / 16;
            int PosX = (int)NPC.position.X / 16;
            int maxXTile = (int)(NPC.position.X + NPC.width) / 16;
            for (int num163 = PosX; num163 <= maxXTile; num163++)
            {
                if (Main.tile[num163, maxYTile] == null)
                {
                    return;
                }
                val2 = Main.tile[num163, maxYTile];
                if (val2.HasUnactuatedTile)
                {
                    bool[] tileSolid = Main.tileSolid;
                    val2 = Main.tile[num163, maxYTile];
                    if (tileSolid[val2.TileType])
                    {
                        isOnSolidTile = true;
                        break;
                    }
                }
            }
        }
        int num164;
        Vector2 position2;
        int num165;
        int num166;
        if (NPC.velocity.Y >= 0f)
        {
            num164 = 0;
            if (NPC.velocity.X < 0f)
            {
                num164 = -1;
            }
            if (NPC.velocity.X > 0f)
            {
                num164 = 1;
            }
            position2 = NPC.position;
            position2.X += NPC.velocity.X;
            num165 = (int)((position2.X + NPC.width / 2 + (NPC.width / 2 + 1) * num164) / 16f);
            num166 = (int)((position2.Y + NPC.height - 1f) / 16f);
            if (num165 * 16 < position2.X + NPC.width && num165 * 16 + 16 > position2.X)
            {
                val2 = Main.tile[num165, num166];
                if (val2.HasUnactuatedTile)
                {
                    val2 = Main.tile[num165, num166];
                    if (!val2.TopSlope)
                    {
                        val2 = Main.tile[num165, num166 - 1];
                        if (!val2.TopSlope)
                        {
                            bool[] tileSolid2 = Main.tileSolid;
                            val2 = Main.tile[num165, num166];
                            if (tileSolid2[val2.TileType])
                            {
                                bool[] tileSolidTop = Main.tileSolidTop;
                                val2 = Main.tile[num165, num166];
                                if (!tileSolidTop[val2.TileType])
                                {
                                    goto IL_079f;
                                }
                            }
                        }
                    }
                }
                val2 = Main.tile[num165, num166 - 1];
                if (val2.IsHalfBlock)
                {
                    val2 = Main.tile[num165, num166 - 1];
                    if (val2.HasUnactuatedTile)
                    {
                        goto IL_079f;
                    }
                }
            }
        }
        goto IL_0a9e;
    IL_0a9e:
        if (isOnSolidTile)
        {
            int doorCheckX = (int)((NPC.position.X + NPC.width / 2 + 15 * NPC.direction) / 16f);
            int doorCheckY = (int)((NPC.position.Y + NPC.height - 15f) / 16f);
            val2 = Main.tile[doorCheckX, doorCheckY - 1];
            int num180;
            if (val2.HasUnactuatedTile)
            {
                val2 = Main.tile[doorCheckX, doorCheckY - 1];
                if (val2.TileType != 10)
                {
                    val2 = Main.tile[doorCheckX, doorCheckY - 1];
                    num180 = val2.TileType == 388 ? 1 : 0;
                }
                else
                {
                    num180 = 1;
                }
            }
            else
            {
                num180 = 0;
            }
            if (((uint)num180 & (flag28 ? 1u : 0u)) != 0)
            {
                NPC.ai[2] += 1f;
                NPC.ai[3] = 0f;
                if (!(NPC.ai[2] >= 60f))
                {
                    return;
                }
                NPC.velocity.X = 0.5f * (0f - NPC.direction);
                int num172 = 5;
                val2 = Main.tile[doorCheckX, doorCheckY - 1];
                if (val2.TileType == 388)
                {
                    num172 = 2;
                }
                NPC.ai[1] += num172;
                NPC.ai[2] = 0f;
                bool flag23 = false;
                if (NPC.ai[1] >= 10f)
                {
                    flag23 = true;
                    NPC.ai[1] = 10f;
                }
                WorldGen.KillTile(doorCheckX, doorCheckY - 1, true, false, false);
                if (!((Main.netMode != NetmodeID.MultiplayerClient || !flag23) && flag23) || Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }
                val2 = Main.tile[doorCheckX, doorCheckY - 1];
                if (val2.TileType == 10)
                {
                    bool flag24 = WorldGen.OpenDoor(doorCheckX, doorCheckY - 1, NPC.direction);
                    if (!flag24)
                    {
                        NPC.ai[3] = backUpTimer;
                        NPC.netUpdate = true;
                    }
                    if (Main.dedServ && flag24)
                    {
                        NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, doorCheckX, doorCheckY - 1, NPC.direction, 0, 0, 0);
                    }
                }
                val2 = Main.tile[doorCheckX, doorCheckY - 1];
                if (val2.TileType == 388)
                {
                    bool flag25 = WorldGen.ShiftTallGate(doorCheckX, doorCheckY - 1, false, false);
                    if (!flag25)
                    {
                        NPC.ai[3] = backUpTimer;
                        NPC.netUpdate = true;
                    }
                    if (Main.dedServ && flag25)
                    {
                        NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 4, doorCheckX, doorCheckY - 1, 0f, 0, 0, 0);
                    }
                }
                return;
            }
            int num173 = NPC.spriteDirection;
            if ((!(NPC.velocity.X < 0f) || num173 != -1) && (!(NPC.velocity.X > 0f) || num173 != 1))
            {
                return;
            }
            if (NPC.height >= 32)
            {
                val2 = Main.tile[doorCheckX, doorCheckY - 2];
                if (val2.HasUnactuatedTile)
                {
                    bool[] tileSolid3 = Main.tileSolid;
                    val2 = Main.tile[doorCheckX, doorCheckY - 2];
                    if (tileSolid3[val2.TileType])
                    {
                        val2 = Main.tile[doorCheckX, doorCheckY - 3];
                        if (val2.HasUnactuatedTile)
                        {
                            bool[] tileSolid4 = Main.tileSolid;
                            val2 = Main.tile[doorCheckX, doorCheckY - 3];
                            if (tileSolid4[val2.TileType])
                            {
                                NPC.velocity.Y = -8f;
                                NPC.netUpdate = true;
                                goto IL_10cc;
                            }
                        }
                        NPC.velocity.Y = -7f;
                        NPC.netUpdate = true;
                        goto IL_10cc;
                    }
                }
            }
            val2 = Main.tile[doorCheckX, doorCheckY - 1];
            if (val2.HasUnactuatedTile)
            {
                bool[] tileSolid5 = Main.tileSolid;
                val2 = Main.tile[doorCheckX, doorCheckY - 1];
                if (tileSolid5[val2.TileType])
                {
                    NPC.velocity.Y = -6f;
                    NPC.netUpdate = true;
                    goto IL_10cc;
                }
            }
            if (NPC.position.Y + NPC.height - doorCheckY * 16 > 20f)
            {
                val2 = Main.tile[doorCheckX, doorCheckY];
                if (val2.HasUnactuatedTile)
                {
                    val2 = Main.tile[doorCheckX, doorCheckY];
                    if (!val2.TopSlope)
                    {
                        bool[] tileSolid6 = Main.tileSolid;
                        val2 = Main.tile[doorCheckX, doorCheckY];
                        if (tileSolid6[val2.TileType])
                        {
                            NPC.velocity.Y = -5f;
                            NPC.netUpdate = true;
                            goto IL_10cc;
                        }
                    }
                }
            }
            if (NPC.directionY >= 0)
            {
                goto IL_10a5;
            }
            val2 = Main.tile[doorCheckX, doorCheckY + 1];
            if (val2.HasUnactuatedTile)
            {
                bool[] tileSolid7 = Main.tileSolid;
                val2 = Main.tile[doorCheckX, doorCheckY + 1];
                if (tileSolid7[val2.TileType])
                {
                    goto IL_10a5;
                }
            }
            val2 = Main.tile[doorCheckX + NPC.direction, doorCheckY + 1];
            if (val2.HasUnactuatedTile)
            {
                bool[] tileSolid8 = Main.tileSolid;
                val2 = Main.tile[doorCheckX + NPC.direction, doorCheckY + 1];
                if (tileSolid8[val2.TileType])
                {
                    goto IL_10a5;
                }
            }
            NPC.velocity.Y = -8f;
            NPC.velocity.X = NPC.velocity.X * 1.5f;
            NPC.netUpdate = true;
            goto IL_10cc;
        }
        if (flag28)
        {
            NPC.ai[1] = 0f;
            NPC.ai[2] = 0f;
        }
        return;
    IL_079f:
        val2 = Main.tile[num165, num166 - 1];
        if (val2.HasUnactuatedTile)
        {
            bool[] tileSolid9 = Main.tileSolid;
            val2 = Main.tile[num165, num166 - 1];
            if (tileSolid9[val2.TileType])
            {
                bool[] tileSolidTop2 = Main.tileSolidTop;
                val2 = Main.tile[num165, num166 - 1];
                if (!tileSolidTop2[val2.TileType])
                {
                    val2 = Main.tile[num165, num166 - 1];
                    if (!val2.IsHalfBlock)
                    {
                        goto IL_0a9e;
                    }
                    val2 = Main.tile[num165, num166 - 4];
                    if (val2.HasUnactuatedTile)
                    {
                        bool[] tileSolid10 = Main.tileSolid;
                        val2 = Main.tile[num165, num166 - 4];
                        if (tileSolid10[val2.TileType])
                        {
                            bool[] tileSolidTop3 = Main.tileSolidTop;
                            val2 = Main.tile[num165, num166 - 4];
                            if (!tileSolidTop3[val2.TileType])
                            {
                                goto IL_0a9e;
                            }
                        }
                    }
                }
            }
        }
        val2 = Main.tile[num165, num166 - 2];
        if (val2.HasUnactuatedTile)
        {
            bool[] tileSolid11 = Main.tileSolid;
            val2 = Main.tile[num165, num166 - 2];
            if (tileSolid11[val2.TileType])
            {
                bool[] tileSolidTop4 = Main.tileSolidTop;
                val2 = Main.tile[num165, num166 - 2];
                if (!tileSolidTop4[val2.TileType])
                {
                    goto IL_0a9e;
                }
            }
        }
        val2 = Main.tile[num165, num166 - 3];
        if (val2.HasUnactuatedTile)
        {
            bool[] tileSolid12 = Main.tileSolid;
            val2 = Main.tile[num165, num166 - 3];
            if (tileSolid12[val2.TileType])
            {
                bool[] tileSolidTop5 = Main.tileSolidTop;
                val2 = Main.tile[num165, num166 - 3];
                if (!tileSolidTop5[val2.TileType])
                {
                    goto IL_0a9e;
                }
            }
        }
        val2 = Main.tile[num165 - num164, num166 - 3];
        if (val2.HasUnactuatedTile)
        {
            bool[] tileSolid13 = Main.tileSolid;
            val2 = Main.tile[num165 - num164, num166 - 3];
            if (tileSolid13[val2.TileType])
            {
                goto IL_0a9e;
            }
        }
        float num167 = num166 * 16;
        val2 = Main.tile[num165, num166];
        if (val2.IsHalfBlock)
        {
            num167 += 8f;
        }
        val2 = Main.tile[num165, num166 - 1];
        if (val2.IsHalfBlock)
        {
            num167 -= 8f;
        }
        if (num167 < position2.Y + NPC.height)
        {
            float num168 = position2.Y + NPC.height - num167;
            float num169 = 16.1f;
            if (num168 <= num169)
            {
                NPC nPC3 = NPC;
                nPC3.gfxOffY += NPC.position.Y + NPC.height - num167;
                NPC.position.Y = num167 - NPC.height;
                if (num168 < 9f)
                {
                    NPC.stepSpeed = 1f;
                }
                else
                {
                    NPC.stepSpeed = 2f;
                }
            }
        }
        goto IL_0a9e;
    IL_10a5:
        if (flag28)
        {
            NPC.ai[1] = 0f;
            NPC.ai[2] = 0f;
        }
        goto IL_10cc;
    IL_10cc:
        if (NPC.velocity.Y == 0f && flag26 && NPC.ai[3] == 1f)
        {
            NPC.velocity.Y = -5f;
        }
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
        {

        }
    }
    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (!spawnInfo.PlayerSafe && spawnInfo.Player.ZoneDesert && Main.hardMode && NoInvasion(spawnInfo.Player))
        {
            return 0.04f;
        }
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
