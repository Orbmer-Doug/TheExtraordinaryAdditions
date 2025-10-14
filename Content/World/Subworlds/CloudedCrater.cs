using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudedCrater : Subworld
{
    public class CloudedCraterPass : GenPass
    {
        public CloudedCraterPass() : base("Terrain", 1f) { }

        public override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            // Set the progress text
            progress.Message = "Blasting a part of the world.";

            // Define the position of the world lines
            Main.worldSurface = Main.maxTilesY - 8;
            Main.rockLayer = Main.maxTilesY - 9;

            // Generate the crater
            CloudedCraterWorldGen.Generate();
        }
    }

    public static TagCompound ClientWorldDataTag
    {
        get;
        internal set;
    }

    public override LocalizedText DisplayName => Language.GetOrRegister("Mods.TheExtraordinaryAdditions.CloudedCrater.DisplayName", null);
    public const int width = 1200;
    public override int Width => width;
    public const int height = 345;
    public override int Height => height;

    // This is mainly so that map data is saved across attempts
    public override bool ShouldSave => true;

    public override List<GenPass> Tasks =>
    [
        new CloudedCraterPass()
    ];

    public override bool ChangeAudio()
    {
        // Get rid of the title screen music when moving between subworlds
        if (Main.gameMenu)
        {
            Main.newMusic = 0;
            return true;
        }

        return false;
    }

    public override void DrawMenu(GameTime gameTime)
    {
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Rectangle target = ToScreenTarget(Vector2.Zero, Main.ScreenSize.ToVector2());
        Main.spriteBatch.Draw(pixel, target, Color.White);
    }

    public static TagCompound SafeWorldDataToTag(string suffix, bool saveInCentralRegistry = true)
    {
        // Re-initialize the save data tag.
        TagCompound savedWorldData = [];

        bool revengeanceMode = CalamityWorld.revenge;
        bool deathMode = CalamityWorld.death;
        if (revengeanceMode)
            savedWorldData["RevengeanceMode"] = revengeanceMode;
        if (deathMode)
            savedWorldData["DeathMode"] = deathMode;

        if (BossDownedSaveSystem.HasDefeated<StygainHeart>())
            savedWorldData["StygainDefeated"] = true;
        if (BossDownedSaveSystem.HasDefeated<Asterlin>())
            savedWorldData["AsterlinDefeated"] = true;

        if (Main.zenithWorld)
            savedWorldData["GFB"] = Main.zenithWorld;

        #region Save Cal Downed State
        List<string> list = new List<string>();
        if (DownedBossSystem.downedDesertScourge)
            list.Add("desertScourge");
        if (DownedBossSystem.downedCrabulon)
            list.Add("crabulon");
        if (DownedBossSystem.downedHiveMind)
            list.Add("hiveMind");
        if (DownedBossSystem.downedPerforator)
            list.Add("perforator");
        if (DownedBossSystem.downedSlimeGod)
            list.Add("slimeGod");
        if (DownedBossSystem.downedDreadnautilus)
            list.Add("dreadnautilus");
        if (DownedBossSystem.downedCryogen)
            list.Add("cryogen");
        if (DownedBossSystem.downedAquaticScourge)
            list.Add("aquaticScourge");
        if (DownedBossSystem.downedBrimstoneElemental)
            list.Add("brimstoneElemental");
        if (DownedBossSystem.downedCalamitasClone)
            list.Add("calamitas");
        if (DownedBossSystem.downedLeviathan)
            list.Add("leviathan");
        if (DownedBossSystem.downedAstrumAureus)
            list.Add("astrageldon");
        if (DownedBossSystem.downedBetsy)
            list.Add("betsy");
        if (DownedBossSystem.downedPlaguebringer)
            list.Add("plaguebringerGoliath");
        if (DownedBossSystem.downedRavager)
            list.Add("scavenger");
        if (DownedBossSystem.downedAstrumDeus)
            list.Add("starGod");
        if (DownedBossSystem.downedGuardians)
            list.Add("guardians");
        if (DownedBossSystem.downedDragonfolly)
            list.Add("bumblebirb");
        if (DownedBossSystem.downedProvidence)
            list.Add("providence");
        if (DownedBossSystem.downedCeaselessVoid)
            list.Add("ceaselessVoid");
        if (DownedBossSystem.downedStormWeaver)
            list.Add("stormWeaver");
        if (DownedBossSystem.downedSignus)
            list.Add("signus");
        if (DownedBossSystem.downedSecondSentinels)
            list.Add("secondSentinels");
        if (DownedBossSystem.downedPolterghast)
            list.Add("polterghast");
        if (DownedBossSystem.downedBoomerDuke)
            list.Add("oldDuke");
        if (DownedBossSystem.downedDoG)
            list.Add("devourerOfGods");
        if (DownedBossSystem.downedYharon)
            list.Add("yharon");
        if (DownedBossSystem.downedThanatos)
            list.Add("thanatos");
        if (DownedBossSystem.downedArtemisAndApollo)
            list.Add("artemisAndApollo");
        if (DownedBossSystem.downedAres)
            list.Add("ares");
        if (DownedBossSystem.downedExoMechs)
            list.Add("exoMechs");
        if (DownedBossSystem.downedCalamitas)
            list.Add("supremeCalamitas");
        if (DownedBossSystem.downedPrimordialWyrm)
            list.Add("adultEidolonWyrm");
        if (DownedBossSystem.downedCLAM)
            list.Add("clam");
        if (DownedBossSystem.downedEoCAcidRain)
            list.Add("eocRain");
        if (DownedBossSystem.downedCLAMHardMode)
            list.Add("clamHardmode");
        if (DownedBossSystem.downedCragmawMire)
            list.Add("cragmawMire");
        if (DownedBossSystem.downedAquaticScourgeAcidRain)
            list.Add("hmRain");
        if (DownedBossSystem.downedGSS)
            list.Add("greatSandShark");
        if (DownedBossSystem.downedMauler)
            list.Add("mauler");
        if (DownedBossSystem.downedNuclearTerror)
            list.Add("nuclearTerror");
        if (DownedBossSystem.startedBossRushAtLeastOnce)
            list.Add("startedBossRush");
        if (DownedBossSystem.downedBossRush)
            list.Add("bossRush");
        savedWorldData["downedFlags"] = list;
        #endregion

        // Store the tag.
        if (saveInCentralRegistry)
            SubworldSystem.CopyWorldData($"CraterSavedWorldData_{suffix}", savedWorldData);

        return savedWorldData;
    }

    public static void LoadWorldDataFromTag(string suffix, TagCompound specialTag = null)
    {
        TagCompound savedWorldData = specialTag ?? SubworldSystem.ReadCopiedWorldData<TagCompound>($"CraterSavedWorldData_{suffix}");

        CalamityWorld.revenge = savedWorldData.ContainsKey("RevengeanceMode");
        CalamityWorld.death = savedWorldData.ContainsKey("DeathMode");

        if (savedWorldData.ContainsKey("StygainDefeated"))
            BossDownedSaveSystem.SetDefeatState<StygainHeart>(true);
        if (savedWorldData.ContainsKey("AsterlinDefeated"))
            BossDownedSaveSystem.SetDefeatState<Asterlin>(true);

        Main.zenithWorld = savedWorldData.ContainsKey("GFB");

        #region Load Cal Downed States
        IList<string> list = savedWorldData.GetList<string>("downedFlags");
        DownedBossSystem.downedDesertScourge = list.Contains("desertScourge");
        DownedBossSystem.downedAquaticScourge = list.Contains("aquaticScourge");
        DownedBossSystem.downedCrabulon = list.Contains("crabulon");
        DownedBossSystem.downedHiveMind = list.Contains("hiveMind");
        DownedBossSystem.downedPerforator = list.Contains("perforator");
        DownedBossSystem.downedSlimeGod = list.Contains("slimeGod");
        DownedBossSystem.downedDreadnautilus = list.Contains("dreadnautilus");
        DownedBossSystem.downedCryogen = list.Contains("cryogen");
        DownedBossSystem.downedBrimstoneElemental = list.Contains("brimstoneElemental");
        DownedBossSystem.downedCalamitasClone = list.Contains("calamitas");
        DownedBossSystem.downedLeviathan = list.Contains("leviathan");
        DownedBossSystem.downedAstrumAureus = list.Contains("astrageldon");
        DownedBossSystem.downedBetsy = list.Contains("betsy");
        DownedBossSystem.downedPlaguebringer = list.Contains("plaguebringerGoliath");
        DownedBossSystem.downedRavager = list.Contains("scavenger");
        DownedBossSystem.downedAstrumDeus = list.Contains("starGod");
        DownedBossSystem.downedGuardians = list.Contains("guardians");
        DownedBossSystem.downedDragonfolly = list.Contains("bumblebirb");
        DownedBossSystem.downedProvidence = list.Contains("providence");
        DownedBossSystem.downedCeaselessVoid = list.Contains("ceaselessVoid");
        DownedBossSystem.downedStormWeaver = list.Contains("stormWeaver");
        DownedBossSystem.downedSignus = list.Contains("signus");
        DownedBossSystem.downedPolterghast = list.Contains("polterghast");
        DownedBossSystem.downedBoomerDuke = list.Contains("oldDuke");
        DownedBossSystem.downedSecondSentinels = list.Contains("secondSentinels");
        DownedBossSystem.downedDoG = list.Contains("devourerOfGods");
        DownedBossSystem.downedYharon = list.Contains("yharon");
        DownedBossSystem.downedThanatos = list.Contains("thanatos");
        DownedBossSystem.downedArtemisAndApollo = list.Contains("artemisAndApollo");
        DownedBossSystem.downedAres = list.Contains("ares");
        DownedBossSystem.downedExoMechs = list.Contains("exoMechs");
        DownedBossSystem.downedCalamitas = list.Contains("supremeCalamitas");
        DownedBossSystem.downedPrimordialWyrm = list.Contains("adultEidolonWyrm");
        DownedBossSystem.downedCLAM = list.Contains("clam");
        DownedBossSystem.downedEoCAcidRain = list.Contains("eocRain");
        DownedBossSystem.downedCLAMHardMode = list.Contains("clamHardmode");
        DownedBossSystem.downedCragmawMire = list.Contains("cragmawMire");
        DownedBossSystem.downedAquaticScourgeAcidRain = list.Contains("hmRain");
        DownedBossSystem.downedGSS = list.Contains("greatSandShark");
        DownedBossSystem.downedMauler = list.Contains("mauler");
        DownedBossSystem.downedNuclearTerror = list.Contains("nuclearTerror");
        DownedBossSystem.startedBossRushAtLeastOnce = list.Contains("startedBossRush");
        DownedBossSystem.downedBossRush = list.Contains("bossRush");
        #endregion
    }

    public override void CopyMainWorldData() => SafeWorldDataToTag("Main");

    public override void ReadCopiedMainWorldData() => LoadWorldDataFromTag("Main");

    public override void CopySubworldData() => SafeWorldDataToTag("Subworld");

    public override void ReadCopiedSubworldData() => LoadWorldDataFromTag("Subworld");

    public override void OnExit()
    {
        for (int i = ScreenShaderUpdates.ShaderEntities.Count - 1; i >= 0; i--)
        {
            IHasScreenShader entity = ScreenShaderUpdates.ShaderEntities[i];
            entity.ReleaseShader();
        }
    }
}