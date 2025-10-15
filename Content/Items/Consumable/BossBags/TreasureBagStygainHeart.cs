using CalamityMod;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

namespace TheExtraordinaryAdditions.Content.Items.Consumable.BossBags;

public class TreasureBagStygainHeart : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TreasureBagStygainHeart);
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
        itemLoot.Add(ModContent.ItemType<RedMistHelmet>());
        itemLoot.Add(ModContent.ItemType<NothingThereHelmet>());
        itemLoot.Add(ModContent.ItemType<MimicryChestplate>());
        itemLoot.Add(ModContent.ItemType<MimicryLeggings>());

        itemLoot.Add(ModContent.ItemType<Sangue>());
        itemLoot.Add(ModContent.ItemType<HemoglobbedCapsule>());
        itemLoot.Add(ModContent.ItemType<LanceOfSanguineSteels>());
        itemLoot.Add(ModContent.ItemType<Exsanguination>());

        itemLoot.Add(StygainHeart.MaskID, 7);
        itemLoot.AddRevBagAccessories();

        itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<StygainHeart>()));
        itemLoot.Add(ModContent.ItemType<BloodOrb>(), 1, 200, 250);
    }
}
