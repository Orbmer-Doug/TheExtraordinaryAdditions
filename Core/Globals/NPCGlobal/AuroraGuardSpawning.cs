using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Globals.NPCGlobal;

public class AuroraGuardSpawning : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return lateInstantiation && entity.type == ItemID.FrostCore;
    }

    public override void PostUpdate(Item item)
    {
        int type = ModContent.NPCType<AuroraGuard>();
        if (item.stack == 1 && !item.beingGrabbed && Collision.DrownCollision(item.position, item.width, item.height, -1f, false)
            && Main.SceneMetrics.EnoughTilesForSnow && Main.raining && item.Center.ToTileCoordinates().Y <= Main.worldSurface && !NPC.AnyNPCs(type))
        {
            for (int i = 0; i < 30; i++)
                ParticleRegistry.SpawnBloomPixelParticle(item.RandAreaInEntity(), -Vector2.UnitY.RotatedByRandom(.4f) * Main.rand.NextFloat(4f, 10f),
                    Main.rand.Next(40, 90), Main.rand.NextFloat(.4f, .8f), Color.DarkBlue, Color.DarkSlateBlue, null, 1f, 4);

            if (item.playerIndexTheItemIsReservedFor == Main.myPlayer)
            {
                if (PlayerTargeting.FindNearestPlayer(item.Center, out Player closest) && closest.Distance(item.Center) < 4800f)
                {
                    Vector2 pos = closest.Center - new Vector2((Main.rand.NextBool() ? -Main.LogicCheckScreenWidth : Main.LogicCheckScreenWidth)
                            + Main.rand.NextFloat(-100f, 100f), 2000f);
                    AdditionsNetcode.NewNPC_ClientSide(pos, type, closest);
                }
                item.Kill();
            }
        }
    }
}