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
        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.AngelsRage.Path,
            AssetRegistry.GennedTextures.AngelsRagePlaced.Path,
            AssetRegistry.GennedSounds.Music.Infinite.SoundPath, out AngelsRageID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.FierceBattle.Path,
            AssetRegistry.GennedTextures.FierceBattlePlaced.Path,
            AssetRegistry.GennedSounds.Music.SRank.SoundPath, out FierceBattleID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.FrigidGale.Path,
            AssetRegistry.GennedTextures.FrigidGalePlaced.Path,
            AssetRegistry.GennedSounds.Music.FrigidGale.SoundPath, out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.Ladikerfos.Path,
            AssetRegistry.GennedTextures.LadikerfosPlaced.Path,
            AssetRegistry.GennedSounds.Music.Ladikerfos.SoundPath, out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.MechanicalInNature.Path,
            AssetRegistry.GennedTextures.MechanicalInNaturePlaced.Path,
            AssetRegistry.GennedSounds.Music.MechanicalInNature.SoundPath, out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.MechanicalInNature2.Path,
            AssetRegistry.GennedTextures.MechanicalInNature2Placed.Path,
            AssetRegistry.GennedSounds.Music.MechanicalInNature2.SoundPath, out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.MenuMusic.Path,
            AssetRegistry.GennedTextures.MenuMusicPlaced.Path,
            AssetRegistry.GennedSounds.Music.Protostar.SoundPath, out MenuMusicID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.RainDance.Path,
            AssetRegistry.GennedTextures.RainDancePlaced.Path,
            AssetRegistry.GennedSounds.Music.RainDance.SoundPath, out RainDanceID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.SereneSatellite.Path,
            AssetRegistry.GennedTextures.SereneSatellitePlaced.Path,
            AssetRegistry.GennedSounds.Music.clairdelune.SoundPath, out SereneSatelliteID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.SnailRoar.Path,
            AssetRegistry.GennedTextures.SnailRoarPlaced.Path,
            AssetRegistry.GennedSounds.Music.sickest_beat_ever.SoundPath, out _);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.SpiderMusic.Path,
            AssetRegistry.GennedTextures.SpiderMusicPlaced.Path,
            AssetRegistry.GennedSounds.Music.Spider.SoundPath, out SpiderMusicID);

        MusicBoxAutoloader.Create(Mod, AssetRegistry.GennedTextures.WereYouFoolin.Path,
            AssetRegistry.GennedTextures.WereYouFoolinPlaced.Path,
            AssetRegistry.GennedSounds.Music.wereyoufoolin.SoundPath, out WereYouFoolinID);
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
