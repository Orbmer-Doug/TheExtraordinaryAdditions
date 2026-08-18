using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Systems;
using Asterlin = TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Asterlin;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudedCrater : Subworld
{
    public class CloudedCraterPass() : GenPass("Terrain", 1f)
    {
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
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

    public static TagCompound ClientWorldDataTag { get; internal set; }

    public override LocalizedText DisplayName =>
        Language.GetOrRegister("Mods.TheExtraordinaryAdditions.CloudedCrater.DisplayName", null);

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
        Texture2D pixel = AssetRegistry.GennedTextures.Pixel;
        Rectangle target = ToScreenTarget(Vector2.Zero, Main.ScreenSize.ToVector2());
        Main.spriteBatch.Draw(pixel, target, Color.White);
    }

    public static TagCompound SafeWorldDataToTag(string suffix, bool saveInCentralRegistry = true)
    {
        // Re-initialize the save data tag.
        TagCompound savedWorldData = [];

        if (BossDownedSaveSystem.HasDefeated<StygainHeart>())
            savedWorldData["StygainDefeated"] = true;
        if (BossDownedSaveSystem.HasDefeated<Asterlin>())
            savedWorldData["AsterlinDefeated"] = true;

        if (Main.zenithWorld)
            savedWorldData["GFB"] = Main.zenithWorld;

        // Store the tag.
        if (saveInCentralRegistry)
            SubworldSystem.CopyWorldData($"CraterSavedWorldData_{suffix}", savedWorldData);

        return savedWorldData;
    }

    public static void LoadWorldDataFromTag(string suffix, TagCompound specialTag = null)
    {
        TagCompound savedWorldData =
            specialTag ?? SubworldSystem.ReadCopiedWorldData<TagCompound>($"CraterSavedWorldData_{suffix}");

        if (savedWorldData.ContainsKey("StygainDefeated"))
            BossDownedSaveSystem.SetDefeatState<StygainHeart>(true);
        if (savedWorldData.ContainsKey("AsterlinDefeated"))
            BossDownedSaveSystem.SetDefeatState<Asterlin>(true);

        Main.zenithWorld = savedWorldData.ContainsKey("GFB");
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
