using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;

namespace TheExtraordinaryAdditions.Core.Globals;

public class AdditionsRecipes : ModSystem
{
    #region Fields
    internal static int AnyCopperBar;

    internal static int AnySilverBar;

    internal static int AnyGoldBar;

    internal static int AnyEvilBar;

    internal static int AnyCobaltBar;

    internal static int AnyMythrilBar;

    internal static int AnyAdamantiteBar;

    internal static int AnyEvilPowder;

    internal static int Boss2Material;

    internal static int CursedFlameIchor;

    internal static int AnyEvilWater;

    internal static int AnyEvilFlask;

    internal static int AnyWoodenSword;

    internal static int AnyHallowedHelmet;

    internal static int AnyHallowedPlatemail;

    internal static int AnyHallowedGreaves;

    internal static int AnyGoldCrown;

    internal static int LunarPickaxe;

    internal static int LunarHamaxe;

    internal static int AnyManaFlower;

    internal static int AnyQuiver;

    internal static int AnyTombstone;

    internal static int AnyWings;

    internal static int AnyButterfly;

    internal static int AnyIronBar;
    #endregion

    #region Edits
    public override void AddRecipes()
    {
        EditVanillaRecipes();

        Recipe.Create(ItemID.AncientBattleArmorMaterial, 1).AddIngredient(ModContent.ItemType<FulguriteInAJar>(), 2).AddTile(TileID.AlchemyTable)
            .Register();
        Recipe.Create(ItemID.FrostCore, 1).AddIngredient(ModContent.ItemType<CracklingFragments>(), 2).AddTile(TileID.MythrilAnvil)
            .Register();
        Recipe.Create(ItemID.BloodMoonMonolith, 1).AddIngredient(ItemID.BloodMoonStarter, 2).AddIngredient(ItemID.GrayBrick, 50).AddTile(TileID.MythrilAnvil)
            .Register();
        Recipe.Create(ItemID.TruffleWorm, 1).AddIngredient(ItemID.GlowingMushroom, 500).AddIngredient(ItemID.Worm, 1).AddIngredient(ItemID.EnchantedNightcrawler, 1).AddCondition(Condition.DownedPlantera).AddTile(TileID.Autohammer)
            .Register();
        Recipe.Create(ItemID.EmpressButterfly, 1).AddRecipeGroup("AnyButterfly", 1).AddIngredient(ItemID.UnicornHorn, 2).AddIngredient(ItemID.PixieDust, 10).AddIngredient(ItemID.SoulofLight, 3).AddCondition(Condition.DownedPlantera).AddTile(TileID.AlchemyTable)
            .Register();
        Recipe.Create(ItemID.ExtendoGrip, 1).AddRecipeGroup("AnySilverBar", 10).AddIngredient(ItemID.Bone, 50).AddIngredient(ItemID.Wire, 20).AddCondition(Condition.DownedSkeletron).AddTile(TileID.Anvils)
            .Register();
        Recipe.Create(ItemID.Spear, 1).AddRecipeGroup("AnyCopperBar", 7).AddIngredient(ItemID.Wood, 15).AddTile(TileID.Anvils)
            .Register();
        Recipe.Create(ItemID.BoneSword, 1).AddRecipeGroup("AnyIronBar", 7).AddIngredient(ItemID.Bone, 20).AddTile(TileID.Anvils)
            .Register();

        if (!Main.remixWorld)
        {
            Recipe.Create(ItemID.Katana, 1).AddRecipeGroup("AnySilverBar", 9).AddRecipeGroup("AnyGoldBar", 3).AddIngredient(ItemID.Silk, 3).AddTile(TileID.Anvils)
                .Register();
        }
    }

    internal static void EditVanillaRecipes()
    {

    }
    #endregion

    #region Groups
    private static void AddOreAndBarRecipeGroups()
    {
        static string GroupName(int id) => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(id);

        RecipeGroup group = new(() => GroupName(ItemID.CopperBar), [ItemID.CopperBar, ItemID.TinBar]);
        AnyCopperBar = RecipeGroup.RegisterGroup("AnyCopperBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.IronBar), [ItemID.IronBar, ItemID.LeadBar]);
        AnyIronBar = RecipeGroup.RegisterGroup("AnyIronBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.SilverBar), [ItemID.SilverBar, ItemID.TungstenBar]);
        AnySilverBar = RecipeGroup.RegisterGroup("AnySilverBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.GoldBar), [ItemID.GoldBar, ItemID.PlatinumBar]);
        AnyGoldBar = RecipeGroup.RegisterGroup("AnyGoldBar", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.AnyEvilBar"), [ItemID.DemoniteBar, ItemID.CrimtaneBar]);
        AnyEvilBar = RecipeGroup.RegisterGroup("AnyEvilBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.CobaltBar), [ItemID.CobaltBar, ItemID.PalladiumBar]);
        AnyCobaltBar = RecipeGroup.RegisterGroup("AnyCobaltBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.MythrilBar), [ItemID.MythrilBar, ItemID.OrichalcumBar]);
        AnyMythrilBar = RecipeGroup.RegisterGroup("AnyMythrilBar", group);

        group = new RecipeGroup(() => GroupName(ItemID.AdamantiteBar), [ItemID.AdamantiteBar, ItemID.TitaniumBar]);
        AnyAdamantiteBar = RecipeGroup.RegisterGroup("AnyAdamantiteBar", group);
    }

    private static void AddEvilBiomeItemRecipeGroups()
    {
        RecipeGroup group = new(() => Language.GetTextValue("Misc.RecipeGroup.AnyEvilPowder"), [ItemID.VilePowder, ItemID.ViciousPowder]);
        AnyEvilPowder = RecipeGroup.RegisterGroup("AnyEvilPowder", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.Boss2Material"), [ItemID.ShadowScale, ItemID.TissueSample]);
        Boss2Material = RecipeGroup.RegisterGroup("Boss2Material", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.CursedFlameIchor"), [ItemID.CursedFlame, ItemID.Ichor]);
        CursedFlameIchor = RecipeGroup.RegisterGroup("CursedFlameIchor", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.AnyEvilWater"), [ItemID.UnholyWater, ItemID.BloodWater]);
        AnyEvilWater = RecipeGroup.RegisterGroup("AnyEvilWater", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.AnyEvilFlask"), [ItemID.FlaskofCursedFlames, ItemID.FlaskofIchor]);
        AnyEvilFlask = RecipeGroup.RegisterGroup("AnyEvilFlask", group);
    }

    private static void AddEquipmentRecipeGroups()
    {
        RecipeGroup group = new(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.WoodenSword), [ItemID.WoodenSword, ItemID.BorealWoodSword,
            ItemID.RichMahoganySword, ItemID.PalmWoodSword, ItemID.EbonwoodSword, ItemID.ShadewoodSword, ItemID.PearlwoodSword, ItemID.AshWoodSword]);
        AnyWoodenSword = RecipeGroup.RegisterGroup("AnyWoodenSword", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.HallowedHelmet), [ItemID.HallowedHelmet,
            ItemID.HallowedHeadgear, ItemID.HallowedMask, ItemID.HallowedHood, ItemID.AncientHallowedHelmet, ItemID.AncientHallowedHeadgear, ItemID.AncientHallowedHood, ItemID.AncientHallowedMask]);
        AnyHallowedHelmet = RecipeGroup.RegisterGroup("AnyHallowedHelmet", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.HallowedPlateMail), [ItemID.HallowedPlateMail, ItemID.AncientHallowedPlateMail]);
        AnyHallowedPlatemail = RecipeGroup.RegisterGroup("AnyHallowedPlatemail", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.HallowedGreaves), [ItemID.HallowedGreaves, ItemID.AncientHallowedGreaves]);
        AnyHallowedGreaves = RecipeGroup.RegisterGroup("AnyHallowedGreaves", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.GoldCrown), [ItemID.GoldCrown, ItemID.PlatinumCrown]);
        AnyGoldCrown = RecipeGroup.RegisterGroup("AnyGoldCrown", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.LunarPickaxe"), [ItemID.SolarFlarePickaxe, ItemID.VortexPickaxe, ItemID.NebulaPickaxe, ItemID.StardustPickaxe]);
        LunarPickaxe = RecipeGroup.RegisterGroup("LunarPickaxe", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.LunarHamaxe"), [ItemID.LunarHamaxeSolar, ItemID.LunarHamaxeVortex, ItemID.LunarHamaxeNebula, ItemID.LunarHamaxeStardust]);
        LunarHamaxe = RecipeGroup.RegisterGroup("LunarHamaxe", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.ManaFlower), [ItemID.ManaFlower, ItemID.ManaCloak, ItemID.MagnetFlower, ItemID.ArcaneFlower]);
        AnyManaFlower = RecipeGroup.RegisterGroup("AnyManaFlower", group);

        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.AnyQuiver"), [ItemID.MagicQuiver, ItemID.MoltenQuiver, ItemID.StalkersQuiver]);
        AnyQuiver = RecipeGroup.RegisterGroup("AnyQuiver", group);

        group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.Tombstone),
        [
        ItemID.Tombstone, ItemID.GraveMarker, ItemID.CrossGraveMarker, ItemID.Headstone, ItemID.Gravestone, ItemID.Obelisk, ItemID.RichGravestone1,
            ItemID.RichGravestone2, ItemID.RichGravestone3, ItemID.RichGravestone4, ItemID.RichGravestone5
        ]);
        AnyTombstone = RecipeGroup.RegisterGroup("AnyTombstone", group);

        int[] barbecuechicken =
        [
        ItemID.AngelWings, ItemID.DemonWings, ItemID.RedsWings, ItemID.ButterflyWings, ItemID.FairyWings, ItemID.HarpyWings, ItemID.BoneWings, ItemID.FlameWings, ItemID.FrozenWings, ItemID.GhostWings,
        ItemID.SteampunkWings, ItemID.LeafWings, ItemID.BatWings, ItemID.BeeWings, ItemID.DTownsWings, ItemID.WillsWings, ItemID.CrownosWings, ItemID.CenxsWings, ItemID.TatteredFairyWings, ItemID.SpookyWings,
        ItemID.Hoverboard, ItemID.FestiveWings, ItemID.BeetleWings, ItemID.FinWings, ItemID.FishronWings, ItemID.MothronWings, ItemID.WingsSolar, ItemID.WingsVortex, ItemID.WingsNebula, ItemID.WingsStardust,
        ItemID.Yoraiz0rWings, ItemID.JimsWings, ItemID.SkiphsWings, ItemID.LokisWings, ItemID.BetsyWings, ItemID.ArkhalisWings, ItemID.LeinforsWings, ItemID.BejeweledValkyrieWing, ItemID.GhostarsWings, ItemID.GroxTheGreatWings,
        ItemID.FoodBarbarianWings, ItemID.SafemanWings, ItemID.CreativeWings, ItemID.RainbowWings, ItemID.LongRainbowTrailWings, ModContent.ItemType<WingsOfTwilight>()
        ];
        group = new RecipeGroup(() => Language.GetTextValue("Misc.RecipeGroup.AnyWings"), barbecuechicken);
        AnyWings = RecipeGroup.RegisterGroup("AnyWings", group);
    }

    private static void AddMiscRecipeGroups()
    {
        RecipeGroup group = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + "Butterfly", [ItemID.GoldButterfly, ItemID.HellButterfly, ItemID.JuliaButterfly, ItemID.MonarchButterfly, ItemID.PurpleEmperorButterfly, ItemID.RedAdmiralButterfly, ItemID.SulphurButterfly, ItemID.TreeNymphButterfly, ItemID.UlyssesButterfly]);
        AnyButterfly = RecipeGroup.RegisterGroup("AnyButterfly", group);
    }

    public override void AddRecipeGroups()
    {
        AddOreAndBarRecipeGroups();
        AddEvilBiomeItemRecipeGroups();
        AddEquipmentRecipeGroups();
        AddMiscRecipeGroups();
    }
    #endregion
}