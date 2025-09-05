using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Novelty;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

namespace TheExtraordinaryAdditions.Core.Globals;

public class NPCDrops : GlobalNPC
{
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        switch (npc.type)
        {
            case NPCID.RuneWizard:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Fireball>(), 1, 1, 1));
                break;
            case NPCID.Paladin:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MythicScrap>(), 1, 6, 12));
                break;
            case NPCID.Turtle | NPCID.SeaTurtle | NPCID.TurtleJungle:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TortoiseShell>(), 17, 1, 1));
                break;
            case NPCID.MoonLordCore:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AshersWhiteTie>(), 1, 1, 1));
                break;
            case NPCID.RedDevil:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FlameInsignia>(), 8, 1, 1));
                break;
            case NPCID.MartianSaucerCore:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrumpledBlueprint>(), 4, 1, 1));
                break;
            case NPCID.Mothron:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EclipsedDuo>(), 4, 1, 1));
                break;
            case NPCID.RustyArmoredBonesAxe | NPCID.RustyArmoredBonesFlail | NPCID.RustyArmoredBonesSword | NPCID.RustyArmoredBonesSwordNoArmor:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Threadripper>(), 8, 1, 1));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WitheredShredder>(), 8, 1, 1));
                break;
            case NPCID.WallofFlesh:
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HellsToothpick>(), 3, 1, 1));
                break;
        }

        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && npc.type == calamityMod.Find<ModNPC>("SupremeCalamitas").Type)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AshyWaterBalloon>(), 1, 69, 69));
        }
    }
}
