global using TheExtraordinaryAdditions.Assets;
global using static TheExtraordinaryAdditions.Core.Utilities.Utility;
global using TheExtraordinaryAdditions.Common.Particles;
global using Microsoft.Xna.Framework;

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

    /// <summary>
    /// TODO: Install EasyNet, the one SLR uses, and go on from there rather than making a big ass switch statement file
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="whoAmI"></param>
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        //AdditionsNetcode.HandlePacket(this, reader, whoAmI);

        if (reader.ReadByte() != 1 || Main.netMode != NetmodeID.Server)
        {
            return;
        }
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodDummyNPC>())
            {
                NPC obj = Main.npc[i];
                obj.life = 0;
                obj.HitEffect(0, 10.0, null);
                obj.SimpleStrikeNPC(int.MaxValue, 0, false, 0f, null, false, 0f, false);
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i, 0f, 0f, 0f, 0, 0, 0);
            }
        }
    }
}