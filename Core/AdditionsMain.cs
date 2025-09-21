global using TheExtraordinaryAdditions.Assets;
global using static TheExtraordinaryAdditions.Core.Utilities.Utility;
global using TheExtraordinaryAdditions.Common.Particles;
global using Microsoft.Xna.Framework;
global using CalUtils = CalamityMod.CalamityUtils; 

// System.Numerics vectors use SIMD, or Single Instruction, Multiple Data. They are more performant because of this.
global using SystemVector2 = System.Numerics.Vector2;
global using SystemVector3 = System.Numerics.Vector3;
global using SystemVector4 = System.Numerics.Vector4;

using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.CrossCompatibility;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.ILEditing;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.Content.Autoloaders;

namespace TheExtraordinaryAdditions.Core;

// TODO: FINISH EVERYTHING AND THEN USE CALAMITY TO SAVE HEADACHE. REMEMBER TO ADD IN COOLDOWN COMPAT AND REPLACE WEAK REFERENCES.
public class AdditionsMain : Mod
{
    public static Mod Instance => ModContent.GetInstance<AdditionsMain>();
    public static readonly LazyAsset<Texture2D> Icon = LazyAsset<Texture2D>.FromPath("TheExtraordinaryAdditions/icon");

    public static bool IsAnniversary()
    {
        return DateTime.Now.Month == 7 && DateTime.Now.Day == 25; //8
    }

    public static bool DoneLoading { get; set; }
    internal Mod bossChecklist;
    public static void SetLoadingText(string text)
    {
        FieldInfo Interface_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface")!.GetField("loadMods", BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo UIProgress_set_SubProgressText = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.UIProgress")!.GetProperty("SubProgressText", BindingFlags.Public | BindingFlags.Instance)!.GetSetMethod()!;

        UIProgress_set_SubProgressText.Invoke(Interface_loadMods.GetValue(null), [text]);
    }

    public override void Load()
    {
        DoneLoading = false;
        bossChecklist = null;
        ModLoader.TryGetMod("BossChecklist", out bossChecklist);

        #region Music Boxes
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.AngelsRage), AssetRegistry.GetTexturePath(AdditionsTexture.AngelsRagePlaced), AssetRegistry.GetMusicPath(AdditionsSound.Infinite));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.FierceBattle), AssetRegistry.GetTexturePath(AdditionsTexture.FierceBattlePlaced), AssetRegistry.GetMusicPath(AdditionsSound.SRank));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.FrigidGale), AssetRegistry.GetTexturePath(AdditionsTexture.FrigidGalePlaced), AssetRegistry.GetMusicPath(AdditionsSound.FrigidGale));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.Ladikerfos), AssetRegistry.GetTexturePath(AdditionsTexture.LadikerfosPlaced), AssetRegistry.GetMusicPath(AdditionsSound.Ladikerfos));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature), AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNaturePlaced), AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature2), AssetRegistry.GetTexturePath(AdditionsTexture.MechanicalInNature2Placed), AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature2));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.MenuMusic), AssetRegistry.GetTexturePath(AdditionsTexture.MenuMusicPlaced), AssetRegistry.GetMusicPath(AdditionsSound.Protostar));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.RainDance), AssetRegistry.GetTexturePath(AdditionsTexture.RainDancePlaced), AssetRegistry.GetMusicPath(AdditionsSound.RainDance));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellite), AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellitePlaced), AssetRegistry.GetMusicPath(AdditionsSound.clairdelune));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.SnailRoar), AssetRegistry.GetTexturePath(AdditionsTexture.SnailRoarPlaced), AssetRegistry.GetMusicPath(AdditionsSound.sickest_beat_ever));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.SpiderMusic), AssetRegistry.GetTexturePath(AdditionsTexture.SpiderMusicPlaced), AssetRegistry.GetMusicPath(AdditionsSound.Spider));
        MusicBoxAutoloader.Create(this, AssetRegistry.GetTexturePath(AdditionsTexture.WereYouFoolin), AssetRegistry.GetTexturePath(AdditionsTexture.WereYouFoolinPlaced), AssetRegistry.GetMusicPath(AdditionsSound.wereyoufoolin));
        #endregion

        SetLoadingText("Loading shaders...");
        AssetRegistry.LoadShaders(this);
        SetLoadingText("Setting up shader recompilation monitor...");
        ShaderRecompilationMonitor.LoadForMod(this);
        SetLoadingText("Initializing screen shaders...");
        Main.QueueMainThreadAction(() =>
        {
            foreach (String key in AssetRegistry.Filters.Keys)
            {
                if (AssetRegistry.Filters.TryGetValue(key, out ManagedScreenShader shader))
                    ScreenShaderPool.InitializePool(filterName: key, baseEffect: shader.Shader, initialCapacity: 10);
            }
        });

        SetLoadingText("Loading boss heads...");
        if (!Main.dedServ)
        {            
            AddBossHeadTexture(AssetRegistry.GetTexturePath(AdditionsTexture.StygainHeart_Head_Boss), -1);
            AddBossHeadTexture(AssetRegistry.GetTexturePath(AdditionsTexture.Asterlin_Head_Boss), -1);
        }

        SetLoadingText("Loading projectile relationships and overrides...");
        BaseIdleHoldoutProjectile.LoadAll();
        ProjectileOverride.LoadAll();
        SetLoadingText("Finished!");
        DoneLoading = true;
    }

    public override void Unload()
    {
        HookHelper.UnloadHooks();

        bossChecklist = null;
    }

    internal static void SaveConfig(AdditionsConfigClient cfg)
    {
        try
        {
            MethodInfo saveMethodInfo = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic);
            if (saveMethodInfo as object != null)
            {
                saveMethodInfo.Invoke(null, [cfg]);
            }
            else
            {
                Instance.Logger.Error("TML ConfigManager.Save reflection failed. Method signature has changed. Notify Additions Devs if you see this in your log.");
            }
        }
        catch
        {
            Instance.Logger.Error("An error occurred while manually saving Additions mod configuration. This may be due to a complex mod conflict. It is safe to ignore this error.");
        }
    }

    public override void PostSetupContent()
    {
        WeakReferenceSupport.Setup();
        HookHelper.LoadHookInterfaces(this);
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        AdditionsNetcode.HandlePackets(this, reader, whoAmI);
    }
}