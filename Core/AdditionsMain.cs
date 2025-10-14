using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.CrossCompatibility;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.ILEditing;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Core;

public class AdditionsMain : Mod
{
    public static Mod Instance => ModContent.GetInstance<AdditionsMain>();
    public static readonly LazyAsset<Texture2D> Icon = LazyAsset<Texture2D>.FromPath("TheExtraordinaryAdditions/icon");

    public static bool IsAnniversary()
    {
        return DateTime.Now.Month == 7 && DateTime.Now.Day == 25; //8
    }

    public static bool DoneLoading { get; set; }
    public static void SetLoadingText(string text)
    {
        FieldInfo Interface_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface")!.GetField("loadMods", BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo UIProgress_set_SubProgressText = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.UIProgress")!.GetProperty("SubProgressText", BindingFlags.Public | BindingFlags.Instance)!.GetSetMethod()!;

        UIProgress_set_SubProgressText.Invoke(Interface_loadMods.GetValue(null), [text]);
    }

    public override void Load()
    {
        DoneLoading = false;

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
            AddBossHeadTexture(AssetRegistry.GetTexturePath(AdditionsTexture.AuroraTurretHead_Head_Boss), -1);
        }

        SetLoadingText("Loading projectile relationships and overrides...");
        BaseIdleHoldoutProjectile.LoadAll();
        ProjectileOverride.LoadAll();

        SetLoadingText("Finished!");
        DoneLoading = true;
    }

    public override void Unload()
    {
        MonoModHooks.RemoveAll(this);
    }

    public override void PostSetupContent()
    {
        WeakReferenceSupport.Setup();
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        AdditionsNetcode.HandlePackets(this, reader, whoAmI);
    }
}