using CalamityMod;
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
        Item.width = Item.height = 32;
        Item.rare = ItemRarityID.Expert;
        Item.expert = true;
    }

    public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) =>
        itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossBags;
    public override bool CanRightClick() => true;
    public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.4f);
    public override void PostUpdate() => Item.TreasureBagLightAndDust();
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) =>
        DrawTreasureBagInWorld(Item, spriteBatch, rotation, scale, whoAmI);

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        itemLoot.Add(ModContent.ItemType<CyberneticRocketGauntlets>());
        itemLoot.Add(ModContent.ItemType<TechnicBlitzripper>());
        itemLoot.Add(ModContent.ItemType<LightripRounds>());
        itemLoot.Add(ModContent.ItemType<TesselesticMeltdown>());
        itemLoot.Add(ModContent.ItemType<LivingStarFlare>());
        itemLoot.AddRevBagAccessories();

        itemLoot.Add(Asterlin.MaskID, 7);

        itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Asterlin>()));
    }
}
