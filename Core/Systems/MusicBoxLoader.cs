using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Autoloaders;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

namespace TheExtraordinaryAdditions.Core.Systems;

public class MusicBoxLoader : ModSystem
{
    private static int AngelsRageID;
    private static int FierceBattleID;
    private static int MenuMusicID;
    private static int RainDanceID;
    private static int SereneSatelliteID;
    private static int SpiderMusicID;
    private static int WereYouFoolinID;

    public override void Load()
    {
        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.AngelsRage),
            AssetRegistry.GetTexturePath(AdditionsTexture.AngelsRagePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.Infinite), out AngelsRageID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.FierceBattle),
            AssetRegistry.GetTexturePath(AdditionsTexture.FierceBattlePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.SRank), out FierceBattleID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.FrigidGale),
            AssetRegistry.GetTexturePath(AdditionsTexture.FrigidGalePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.FrigidGale), out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.Ladikerfos),
            AssetRegistry.GetTexturePath(AdditionsTexture.LadikerfosPlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.Ladikerfos), out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature),
            AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNaturePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature), out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature2),
            AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature2Placed),
            AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature2), out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.MenuMusic),
            AssetRegistry.GetTexturePath(AdditionsTexture.MenuMusicPlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.Protostar), out MenuMusicID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.RainDance),
            AssetRegistry.GetTexturePath(AdditionsTexture.RainDancePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.RainDance), out RainDanceID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellite),
            AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellitePlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.clairdelune), out SereneSatelliteID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.SnailRoar),
            AssetRegistry.GetTexturePath(AdditionsTexture.SnailRoarPlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.sickest_beat_ever), out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.SpiderMusic),
            AssetRegistry.GetTexturePath(AdditionsTexture.SpiderMusicPlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.Spider), out SpiderMusicID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GetTexturePath(AdditionsTexture.WereYouFoolin),
            AssetRegistry.GetTexturePath(AdditionsTexture.WereYouFoolinPlaced),
            AssetRegistry.GetMusicPath(AdditionsSound.wereyoufoolin), out WereYouFoolinID);
    }

    public override void AddRecipes()
    {
        Recipe recipe = Recipe.Create(AngelsRageID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ModContent.ItemType<JudgeOfHellsArmaments>())
            .AddTile(TileID.LunarCraftingStation);
        recipe.Register();

        recipe = Recipe.Create(FierceBattleID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ItemID.SpikyBall, 20)
            .AddIngredient(ItemID.GoldenPlatform, 20)
            .AddIngredient(ItemID.LunarTabletFragment, 20)
            .AddIngredient(ItemID.SpookyWood, 20)
            .AddIngredient(ItemID.IceBlock, 20)
            .AddIngredient(ItemID.MartianConduitPlating, 20)
            .AddTile(TileID.LunarMonolith);
        recipe.Register();

        recipe = Recipe.Create(MenuMusicID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ModContent.ItemType<FlagPole>())
            .AddTile(TileID.Anvils);
        recipe.Register();

        recipe = Recipe.Create(RainDanceID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ItemID.SandBlock, 120)
            .AddIngredient(ItemID.Seashell, 5)
            .AddCondition(Condition.InBeach)
            .AddCondition(Condition.NearWater);
        recipe.Register();

        recipe = Recipe.Create(SereneSatelliteID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ItemID.Moonglow, 10)
            .AddTile(TileID.BloodMoonMonolith);
        recipe.Register();

        recipe = Recipe.Create(SpiderMusicID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ItemID.SpiderFang, 12)
            .AddTile(TileID.Cobweb);
        recipe.Register();

        recipe = Recipe.Create(WereYouFoolinID)
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ItemID.CopperBrick, 10)
            .AddIngredient(ItemID.YellowPaint, 15)
            .AddIngredient(ItemID.Glass, 10)
            .AddIngredient(ItemID.Wire, 30)
            .AddTile(TileID.Anvils);
        recipe.Register();
    }
}
