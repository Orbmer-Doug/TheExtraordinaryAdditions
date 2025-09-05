using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Head)]
public class AbsoluteGreathelm : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbsoluteGreathelm);
    public static int HeadSlotID
    {
        get;
        private set;
    }
    public override void SetStaticDefaults()
    {
        HeadSlotID = Item.headSlot;
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 26;
        Item.defense = 40;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<AbsoluteCoreplate>())
        {
            return legs.type == ModContent.ItemType<AbsoluteGreaves>();
        }
        return false;
    }

    public override void UpdateArmorSet(Player player)
    {
        player.GetAttackSpeed<MeleeDamageClass>() += 0.15f;

        player.ignoreWater = true;
        player.aggro += 1500;
        player.manaCost *= .75f;
        player.statManaMax2 += 90;
        player.maxMinions += 4;
        player.maxTurrets += 2;

        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        if (AdditionsKeybinds.SetBonusHotKey.JustPressed)
        {
            player.NewPlayerProj(player.Center, Vector2.Zero, ModContent.ProjectileType<WhiteVoid>(), (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(7000), 0f, Main.myPlayer);
            player.Additions().AbsoluteCounter = SecondsToFrames(10);
        }
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, Color.AntiqueWhite.ToVector3() * 1.5f);

        ref StatModifier damage = ref player.GetDamage<GenericDamageClass>();
        damage += 0.35f;
        player.GetCritChance<GenericDamageClass>() += 25f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();

        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar)
            && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence)
            && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity)
            && calamityMod.TryFind("GalacticaSingularity", out ModItem GalacticaSingularity)
            && calamityMod.TryFind("LifeAlloy", out ModItem LifeAlloy)
            && calamityMod.TryFind("RuinousSoul", out ModItem RuinousSoul)
            && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil))
        {
            recipe.AddIngredient(ItemID.CrimsonHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltHelmet>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteMask>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTopHat>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorGreathelm>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareHelmet, 1);
            recipe.AddIngredient(CoreofCalamity.Type, 2);
            recipe.AddIngredient(GalacticaSingularity.Type, 4);
            recipe.AddIngredient(LifeAlloy.Type, 5);
            recipe.AddIngredient(RuinousSoul.Type, 5);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 3);
            recipe.AddIngredient(AuricBar.Type, 10);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.CrimsonHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltHelmet>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteMask>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTopHat>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorGreathelm>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareHelmet, 1);
            recipe.AddTile(TileID.LunarCraftingStation);
        }

        recipe.Register();
    }
}