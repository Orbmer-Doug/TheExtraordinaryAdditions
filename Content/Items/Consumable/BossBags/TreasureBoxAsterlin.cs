using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Consumable.BossBags;

public class TreasureBoxAsterlin : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TreasureBoxAsterlin);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        ItemID.Sets.BossBag[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.maxStack = 999;
        Item.consumable = true;
        Item.width = 44;
        Item.height = 38;
        Item.expert = true;
        Item.rare = ItemRarityID.Expert;
    }

    public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
    {
        itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
    }

    public override bool CanRightClick()
    {
        return true;
    }

    public override Color? GetAlpha(Color lightColor)
    {
        return Color.Lerp(lightColor, Color.White, 0.4f);
    }

    public override void PostUpdate() => Item.TreasureBagLightAndDust();

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        return DrawTreasureBagInWorld(Item, spriteBatch, rotation, scale, whoAmI);
    }

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        itemLoot.Add(ModContent.ItemType<CyberneticRocketGauntlets>());
        itemLoot.Add(ModContent.ItemType<TechnicBlitzripper>());
        itemLoot.Add(ModContent.ItemType<LightripRounds>());
        itemLoot.Add(ModContent.ItemType<TesselesticMeltdown>());
        itemLoot.Add(ModContent.ItemType<LivingStarFlare>());

        itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Asterlin>() / 2));
    }
}
