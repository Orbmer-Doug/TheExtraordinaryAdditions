using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using TheExtraordinaryAdditions.Content.Items.Placeable.Base;
using TheExtraordinaryAdditions.Extraordinary.CrossCompatibility;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudedCrater : Subworld
{
    public class CloudedCraterPass : GenPass
    {
        public CloudedCraterPass() : base("Terrain", 1f) { }

        public override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            // Set the progress text.
            progress.Message = "Blasting a part of the world.";

            // Define the position of the world lines.
            Main.worldSurface = Main.maxTilesY - 8;
            Main.rockLayer = Main.maxTilesY - 9;

            // Generate the crater.
            CloudedCraterWorldGen.Generate();
        }
    }

    public override void Load()
    {
        MusicBoxAutoloader.Create(Mod, AssetRegistry.AutoloadedPrefix + "MechanicalInNature", AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature), out _, out _);
    }

    public static TagCompound ClientWorldDataTag
    {
        get;
        internal set;
    }

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
        // Reinitialize the save data tag
        TagCompound savedWorldData = [];

        // Save difficulty data
        bool revengeanceMode = CommonCalamityVariables.RevengeanceModeActive;
        bool deathMode = CommonCalamityVariables.DeathModeActive;
        if (revengeanceMode)
            savedWorldData["RevengeanceMode"] = revengeanceMode;
        if (deathMode)
            savedWorldData["DeathMode"] = deathMode;
        if (Main.zenithWorld)
            savedWorldData["GFB"] = Main.zenithWorld;

        // Save Calamity's boss defeat data
        CommonCalamityVariables.SaveDefeatStates(savedWorldData);

        // Store the tag
        if (saveInCentralRegistry)
            SubworldSystem.CopyWorldData($"CraterSavedWorldData_{suffix}", savedWorldData);

        return savedWorldData;
    }

    public static void LoadWorldDataFromTag(string suffix, TagCompound specialTag = null)
    {
        TagCompound savedWorldData = specialTag ?? SubworldSystem.ReadCopiedWorldData<TagCompound>($"CraterSavedWorldData_{suffix}");

        // Load difficulty data
        CommonCalamityVariables.RevengeanceModeActive = savedWorldData.ContainsKey("RevengeanceMode");
        CommonCalamityVariables.DeathModeActive = savedWorldData.ContainsKey("DeathMode");
        Main.zenithWorld = savedWorldData.ContainsKey("GFB");

        // Load defeat states
        CommonCalamityVariables.LoadDefeatStates(savedWorldData);
    }

    public override void CopyMainWorldData() => SafeWorldDataToTag("Main");

    public override void ReadCopiedMainWorldData() => LoadWorldDataFromTag("Main");

    public override void CopySubworldData() => SafeWorldDataToTag("Subworld");

    public override void ReadCopiedSubworldData() => LoadWorldDataFromTag("Subworld");
}