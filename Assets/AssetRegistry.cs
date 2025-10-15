using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Assets;

public readonly struct LazyAsset<T>(Func<Asset<T>> assetLoadFunction, string path) where T : class
{
    private readonly Lazy<Asset<T>> asset = new Lazy<Asset<T>>(assetLoadFunction);

    public Asset<T> Asset => asset.Value;

    public bool Uninitialized => asset is null;

    public T Value => asset.Value.Value;

    public readonly string Path = path;

    public static LazyAsset<T> FromPath(string path, AssetRequestMode requestMode = AssetRequestMode.AsyncLoad)
    {
        return new LazyAsset<T>(() => ModContent.Request<T>(path, requestMode), path);
    }

    public static implicit operator T(LazyAsset<T> asset) => asset.Value;
}

public readonly struct AssetInfo<T>(T asset, string path)
{
    public readonly T Asset = asset;
    public readonly string Path = path;
}

/// <summary>
/// The central class for maintaining the loading of all sounds, textures, shaders, and filters within the mod
/// </summary>

// If you are a lost soul like I was once trying to find noises on google
// Use this (albeit limited) texture maker:
// https://mebiusbox.github.io/contents/EffectTextureMaker/
// And alternatively the only free magic circle maker i could reliably find:
// https://kpierzynski.github.io/dnd_runes_gen/
// Otherwise just use some alpha drawing on some texture editor or somethin
public class AssetRegistry : ModSystem
{
    public const string TexturePath = "Assets/Textures";
    public const string AudioPath = "Assets/Audio";
    public const string AutoloadDirectoryShaders = "AutoloadedEffects/Shaders";
    public const string AutoloadDirectoryFilters = "AutoloadedEffects/Filters";
    public const string AutoloadedPrefix = "TheExtraordinaryAdditions/Assets/AutoloadedContent/";
    public const string UIPrefix = "TheExtraordinaryAdditions/UI/";

    // Lazy-loaded main asset dictionaries
    public static readonly Dictionary<AdditionsTexture, LazyAsset<Texture2D>> Textures = [];
    public static readonly Dictionary<AdditionsSound, Lazy<AssetInfo<SoundStyle>>> Sounds = [];

    // TODO: Consider making these lazy-loaded...
    public static readonly Dictionary<string, ManagedShader> Shaders = [];
    public static readonly Dictionary<string, ManagedScreenShader> Filters = [];

    public static bool HasFinishedLoadingShaders { get; internal set; }
    public static bool HasFinishedLoading { get; internal set; }

    public override void Unload()
    {
        if (Main.dedServ)
            return;

        Main.QueueMainThreadAction(() =>
        {
            foreach (ManagedShader shader in Shaders.Values)
                shader.Dispose();
            foreach (ManagedScreenShader filter in Filters.Values)
                filter.Dispose();

            Textures.Clear();
            Sounds.Clear();
            Shaders.Clear();
            Filters.Clear();
        });
    }

    public static void InitializeAssetDictionaries(Mod mod)
    {
        HasFinishedLoading = false;
        List<string> fileNames = mod.GetFileNames() ?? [];

        List<string> textureFiles = fileNames.Where(f => f.Contains(TexturePath) && f.EndsWith(".rawimg") && !f.Contains("Container")).ToList();
        List<string> soundFiles = fileNames.Where(f => f.Contains(AudioPath)).ToList();

        foreach (string path in textureFiles)
        {
            string name = Path.GetFileNameWithoutExtension(path).Replace($"{mod.Name}.", string.Empty);
            string clearedPath = $"{mod.Name}/{path.Replace(".rawimg", "")}";
            if (Enum.TryParse<AdditionsTexture>(name, out AdditionsTexture texture))
            {
                Textures[texture] = LazyAsset<Texture2D>.FromPath(clearedPath, AssetRequestMode.ImmediateLoad);
            }
            else
            {
                mod.Logger.Warn($"Texture '{name}' not found in AdditionsTexture enum.");
            }
        }

        foreach (string path in soundFiles)
        {
            string name = Path.GetFileNameWithoutExtension(path).Replace($"{mod.Name}.", string.Empty);
            string clearedPath = $"{mod.Name}/{Path.Combine(Path.GetDirectoryName(path), name).Replace(@"\", "/")}";
            if (Enum.TryParse<AdditionsSound>(name, out AdditionsSound sound))
            {
                SoundType type = clearedPath.Contains("Music") ? SoundType.Music : clearedPath.Contains("Ambient") ? SoundType.Sound : SoundType.Sound;
                Sounds[sound] = new Lazy<AssetInfo<SoundStyle>>(() =>
                    new AssetInfo<SoundStyle>(new SoundStyle(clearedPath, type), clearedPath));
            }
            else
            {
                mod.Logger.Warn($"Sound '{name}' not found in AdditionsSound enum.");
            }
        }
        HasFinishedLoading = true;
    }

    /// <summary>
    /// Sets a shader with a given name in the registry manually.
    /// </summary>
    /// <param name="name">The name of the shader.</param>
    /// <param name="newShaderData">The shader data reference to save.</param>
    public static void SetShader(string name, Ref<Effect> newShaderData) => Shaders[name] = new(name, newShaderData);

    /// <summary>
    /// Sets a filter with a given name in the registry manually.
    /// </summary>
    /// <param name="name">The name of the filter.</param>
    /// <param name="newShaderData">The shader data reference to save.</param>
    public static void SetFilter(string name, Ref<Effect> newShaderData) => Filters[name] = new(newShaderData);
    internal static void LoadShaders(Mod mod)
    {
        HasFinishedLoadingShaders = false;
        if (Main.dedServ)
            return;

        List<string> fileNames = mod.GetFileNames();
        if (fileNames is null)
            return;

        #region Shaders
        IEnumerable<string> shaderLoadPaths = fileNames.Where(path => path.Contains(AutoloadDirectoryShaders)
        && !path.Contains("Compiler/") && (path.Contains(".xnb") || path.Contains(".fxc")));
        IEnumerable<string> shaderFxPathsToCompile = fileNames.Where(path => path.Contains(AutoloadDirectoryShaders)
        && !path.Contains("Compiler/") && path.Contains(".fx") && !shaderLoadPaths.Contains(path.Replace(".fx", ".xnb")));

        foreach (string path in shaderLoadPaths)
        {
            string shaderName = Path.GetFileNameWithoutExtension(path);
            string clearedPath = Path.Combine(Path.GetDirectoryName(path), shaderName).Replace(@"\", @"/").Replace($"{mod.Name}.", string.Empty);
            Ref<Effect> shader = new(mod.Assets.Request<Effect>(clearedPath, AssetRequestMode.ImmediateLoad).Value);
            SetShader(shaderName, shader);
        }

        foreach (string path in shaderFxPathsToCompile)
        {
            string fxPath = mod.Name + "\\" + Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(path)).Replace(@"\", @"/");
            ShaderRecompilationMonitor.CompilingFiles.Enqueue(new(Path.Combine(Main.SavePath, "ModSources", fxPath), false));
        }
        #endregion

        #region Filters
        IEnumerable<string> filterLoadPaths = fileNames.Where(path => path.Contains(AutoloadDirectoryFilters)
        && !path.Contains("Compiler/") && (path.Contains(".xnb") || path.Contains(".fxc")));
        IEnumerable<string> filterFxPathsToCompile = fileNames.Where(path => path.Contains(AutoloadDirectoryFilters)
        && !path.Contains("Compiler/") && path.Contains(".fx") && !filterLoadPaths.Contains(path.Replace(".fx", ".xnb")));

        foreach (string path in filterLoadPaths)
        {
            string filterName = Path.GetFileNameWithoutExtension(path);
            string clearedPath = Path.Combine(Path.GetDirectoryName(path), filterName).Replace(@"\", @"/").Replace($"{mod.Name}.", string.Empty);
            Ref<Effect> filter = new(mod.Assets.Request<Effect>(clearedPath, AssetRequestMode.ImmediateLoad).Value);

            SetFilter(filterName, filter);
        }

        foreach (string path in filterFxPathsToCompile)
        {
            string fxPath = mod.Name + "\\" + Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(path)).Replace(@"\", @"/");
            ShaderRecompilationMonitor.CompilingFiles.Enqueue(new(Path.Combine(Main.SavePath, "ModSources", fxPath), true));
        }
        #endregion
        HasFinishedLoadingShaders = true;
    }

    // Asset access methods
    public static T GetAsset<T, TEnum>(TEnum assetEnum) where TEnum : Enum
    {
        if (typeof(T) == typeof(Texture2D) && assetEnum is AdditionsTexture texture)
            return (T)(object)Textures[texture].Value;
        if (typeof(T) == typeof(SoundStyle) && assetEnum is AdditionsSound sound)
            return (T)(object)Sounds[sound].Value.Asset;
        throw new ArgumentException($"Unsupported asset type: {typeof(T)}");
    }

    public static bool TryGetAsset<T, TEnum>(TEnum assetEnum, out T asset) where TEnum : Enum
    {
        asset = default;
        try
        {
            asset = GetAsset<T, TEnum>(assetEnum);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetAssetPath<TEnum>(TEnum assetEnum) where TEnum : Enum
    {
        if (assetEnum is AdditionsTexture texture)
            return Textures[texture].Path;
        if (assetEnum is AdditionsSound sound)
            return Sounds[sound].Value.Path;
        throw new ArgumentException($"Unsupported asset type for {assetEnum}");
    }

    public static SoundStyle GetSound(AdditionsSound sound) => GetAsset<SoundStyle, AdditionsSound>(sound);
    public static string GetSoundPath(AdditionsSound sound) => GetAssetPath(sound);
    public static string GetMusicPath(AdditionsSound sound) => "Assets/Audio/Music/" + sound.ToString();
    public static Texture2D GetTexture(AdditionsTexture texture) => GetAsset<Texture2D, AdditionsTexture>(texture);
    public static string GetTexturePath(AdditionsTexture texture) => GetAssetPath(texture);
    public static string Invis => GetTexturePath(AdditionsTexture.Invisible);
    public static Texture2D InvisTex => GetTexture(AdditionsTexture.Invisible);

    /// <remarks>
    /// In this context, the "name" must correspond with the file name of the shader, not including the path extension
    /// </remarks>
    public static ManagedShader GetShader(string name) => Shaders[name];

    /// <inheritdoc cref="GetShader(string)"/>
    public static ManagedScreenShader GetFilter(string name) => Filters[name];

    /// <inheritdoc cref="GetShader(string)"/>
    public static bool TryGetShader(string name, out ManagedShader shader) => Shaders.TryGetValue(name, out shader);

    /// <inheritdoc cref="GetShader(string)"/>
    public static bool TryGetFilter(string name, out ManagedScreenShader filter) => Filters.TryGetValue(name, out filter);
}

#region the wall

public enum AdditionsTexture
{
    #region Achievements
    DefeatedAsterlin,
    DefeatedAuroraGuard,
    DefeatedSnail,
    DefeatedStygain,
    ObtainedCube,
    ObtainedGenedies,
    #endregion

    #region AsterlinBackgrounds
    Background_AstralInfection,
    Background_BEES,
    Background_Blizzard,
    Background_BloodMoon,
    Background_Brimstone,
    Background_Cavern,
    Background_CloudedCrater,
    Background_Corruption,
    Background_Crimson,
    Background_Desert,
    Background_Dungeon,
    Background_EternalGarden,
    Background_FrostMoon,
    Background_GemCave,
    Background_GlowingShrooms,
    Background_Goblin,
    Background_Graveyard,
    Background_Hallow,
    Background_Jungle,
    Background_JungleTemple,
    Background_Marble,
    Background_Martian,
    Background_Meteor,
    Background_NebularPillar,
    Background_NotPurpleGranite,
    Background_Ocean,
    Background_OldOnesArmy,
    Background_Pirates,
    Background_PumpkinMoon,
    Background_Purity,
    Background_Rain,
    Background_Sandstorm,
    Background_Shimmer,
    Background_Slime,
    Background_Snail,
    Background_Snow,
    Background_SolarEclipse,
    Background_SolarPillar,
    Background_Space,
    Background_SpiderNest,
    Background_StardustPillar,
    Background_Sulphur,
    Background_SunkenSea,
    Background_Thunder,
    Background_Undergound,
    Background_Underworld,
    Background_VortexPillar,
    #endregion

    #region Grayscale
    BaseStar,
    BasicCircle,
    BasicCircularCircle,
    BasicLine,
    BasicOctogon,
    BasicTriangle,
    BlackHoleSwirl,
    BloomFlare,
    BloomLine,
    BloomLineSmall,
    BloomLineThin,
    BloomLineCap,
    BloomLineHoriz,
    BloomRing,
    BrightLight,
    Bubble,
    Cancer,
    Capricorn,
    CircularSmear,
    CloudDensityMap,
    DimLight,
    DimTrail,
    EmpressStar,
    FourPointedStar,
    Gleam,
    Glow,
    GlowHarsh,
    GlowParticle,
    GlowParticleSmall,
    GlowRing,
    GlowSoft,
    HollowCircleFancy,
    HollowCircleHighRes,
    HollowCircleSoftEdge,
    Invisible,
    LensStar,
    Light,
    Line,
    Mist,
    NebulaGas1,
    NebulaGas2,
    NebulaGas3,
    Pixel,
    Sagittarius,
    scope,
    ScopeGrad,
    SemiCircularSmear,
    SimpleGradient,
    Smoke,
    StarTrail,
    Sun,
    SunGray,
    ThinEndedLine,
    TileGlow1,
    TornadoProj,
    TriTell,
    Vignette,
    #endregion

    #region Noises
    BigWavyBlobNoise,
    BlobbyNoise,
    BlueNebula,
    BlueNebula2,
    CausticNoise,
    Cosmos,
    Cosmos2,
    CrackedNoise,
    CrackedNoise2,
    DarkRidgeNoise,
    DarkTurbulentNoise,
    DendriticNoise,
    DendriticNoiseDim,
    DendriticNoiseZoomedOut,
    FireNoise,
    FlameMap1,
    FlameMap2,
    FluidPerlin,
    FractalNoise,
    HarshNoise,
    LemniscateDistanceLookup,
    LichenNoise,
    ManifoldNoise,
    MeltNoise,
    MercuryNoise,
    NeuronNoise,
    noise,
    OrganicNoise,
    Perlin,
    PerlinCloud,
    PurpleNebula,
    PurpleNebulaBright,
    PurpleNebulaMixed,
    SharpNoise,
    Starfield1,
    Starfield2,
    SuperPerlin,
    SuperWavyPerlin,
    SwordSlashTexture,
    TechyNoise,
    TurbulenceNoise,
    TurbulentNoise,
    TurbulentNoise2,
    VoronoiShapes,
    VoronoiShapes2,
    WarpMap,
    WaterNoise,
    WavyBlotchNoise,
    WavyNeurons,
    #endregion

    #region Trails
    DoubleTrail,
    Lightning2,
    LightningGlow,
    LightningGlowVertical,
    ShadowTrail,
    SlashTrail,
    SlashTrailElectric,
    Spikes,
    Streak,
    Streak2,
    StreakFire,
    StreakLightning,
    StreakMagma,
    Trail,
    TrailThin,
    #endregion

    #region Items
    #region Consumable
    #region BossBags
    TreasureBagStygainHeart,
    TreasureBoxAsterlin,
    #endregion
    AridFlask,
    FrigidTonic,
    SupremeWaterbreathingPotion,
    #endregion
    #region Equipable
    #region Accessories
    #region Early
    FulminicEye,
    FulminicEye_Glow,
    FungalSatchel,
    PrimevalToolkit,
    TungstenCube,
    #endregion
    #region Late
    AncientBoon,
    AshersWhiteTie,
    AshersWhiteTie_Neck,
    CryogenicSpaceCanister,
    GodGauntlet,
    GodGauntlet_Wings,
    TungstenTie,
    #endregion
    #region Middle
    BandOfSunrays,
    EclipsedOnesCloak,
    EclipsedOnesCloak_Front,
    FlameInsignia,
    NitrogenCoolingPack,
    RejuvenationArtifact,
    Rimesplitter,
    WingsOfTwilight,
    WingsOfTwilight_Wings,
    #endregion
    #endregion
    #region Armors
    #region Early
    PetersUshanka,
    PetersUshanka_Head,
    VoltChestplate,
    VoltChestplate_Body,
    VoltGrieves,
    VoltGrieves_Legs,
    VoltHelmet,
    VoltHelmet_Head,
    #endregion
    #region Late
    AbsoluteCoreplate,
    AbsoluteCoreplate_Body,
    AbsoluteGreathelm,
    AbsoluteGreathelm_Head,
    AbsoluteGreaves,
    AbsoluteGreaves_Legs,
    #endregion
    #region Middle
    BlueLeggings,
    BlueLeggings_Legs,
    BlueTopHat,
    BlueTopHat_Head,
    BlueTuxedo,
    BlueTuxedo_Body,
    MimicryChestplate,
    MimicryChestplate_Body,
    MimicryLeggings,
    MimicryLeggings_Legs,
    NothingThereHelmet,
    NothingThereHelmet_Head,
    RedMistHelmet,
    RedMistHelmet_Head,
    SpecteriteChestPiece,
    SpecteriteChestPiece_Body,
    SpecteriteGreaves,
    SpecteriteGreaves_Legs,
    SpecteriteMask,
    SpecteriteMask_Head,
    TremorGreathelm,
    TremorGreathelm_Head,
    TremorPlating,
    TremorPlating_Body,
    TremorSheathe,
    TremorSheathe_Legs,
    #endregion
    #endregion
    #region Pets
    CrimsonCalamari,
    JellyfishSnack,
    Lifeseed,
    PaintCoveredCamera,
    SmallGear,
    #endregion
    #region Vanity
    AsterlinMask,
    AsterlinMask_Head,
    AvatarDress,
    AvatarDress_Body,
    AvatarHorns,
    AvatarHorns_Head,
    AvatarLeggings,
    AvatarLeggings_Legs,
    EclipsedOnesHat,
    EclipsedOnesHat_Head,
    EclipsedOnesLeggings,
    EclipsedOnesLeggings_Legs,
    StygainHeartMask,
    StygainHeartMask_Head,
    #endregion
    #endregion
    #region Materials
    #region Early
    ShockCatalyst,
    ShockCatalyst_Glow,
    #endregion
    #region Late
    FerrymansToken,
    #endregion
    #region Middle
    CracklingFragments,
    CrumpledBlueprint,
    EmblazenedEmber,
    FulguriteInAJar,
    MythicScrap,
    PlasmaCore,
    PlasmaCore_Glow,
    StygianEyeball,
    TremorAlloy,
    WrithingLight,
    #endregion
    #endregion
    #region Novelty
    AshyWaterBalloon,
    TortoiseShell,
    #endregion
    #region Placeable
    AngelsRage,
    AsterlinRelic,
    FierceBattle,
    FlagPole,
    FrigidGale,
    GreenBlock,
    Ladikerfos,
    LockedCyberneticSword,
    MechanicalInNature,
    MechanicalInNature2,
    MenuMusic,
    MeteorBlock,
    Polarity,
    RainDance,
    SereneSatellite,
    SnailRoar,
    SpiderMusic,
    StygainHeartRelic,
    StygainHeartTrophy,
    TechnicTransmitter,
    WereYouFoolin,
    #endregion
    #region Summon
    CrimsonCarvedBeetle,
    TomeOfArchivalScripts,
    TVRemote,
    #endregion
    #region Tools
    BiomeFinder,
    BriefcaseOfBees,
    GodDummy,
    IndustrialBlastDartKit,
    MatterDisintegrationCannon,
    MatterDisintegrationCannonBloom,
    PortableDONTTOUCHME,
    #endregion
    #region Weapons
    #region Classless
    CrossDisc,
    Eagle500kgBomb,
    #endregion
    #region Cynosure
    DivineSpiritCatalyst,
    #endregion
    #region Magic
    #region Early
    BrewingStorms,
    NoxiousSnare,
    TomeOfHellfire,
    #endregion
    #region Late
    CometStorm,
    Epidemic,
    HollowPurple,
    PyroclasticVeil,
    RealitySeamstressesGlove,
    SuperheatedPlasmaArray,
    TesselesticMeltdown,
    TesselesticMeltdown_Glowmask,
    #endregion
    #region Middle
    Acheron,
    BloodFracture,
    Fireball,
    IceMistStaff,
    LanceOfSanguineSteels,
    StarlessSea,
    VirulentEntrapment,
    #endregion
    #endregion
    #region Melee
    #region Early
    BirchStick,
    Fork,
    MeteorKatana,
    ObsidianFlail,
    #endregion
    #region Late
    CallerOfBirds,
    CondereFulmina,
    Cosmireaper,
    Cosmireaper_Proj,
    CyberneticRocketGauntlets,
    EverbladedHeaven,
    FinalStrike,
    HeavenForgedSword,
    RendedStar,
    Sunspot,
    Sunspot_Glow,
    TheSpoon,
    TorrentialTides,
    TripleKatanas,
    #endregion
    #region Middle
    AlucardsSword,
    Bergcrusher,
    BirchTree,
    ComicallyLargeKnife,
    CryingEyeOfCthulhu,
    DecayingCutlery,
    EtherealClaymore,
    HellsToothpick,
    ImpureAstralKatanas,
    JudgeOfHellsArmaments,
    Mimicry,
    RejuvenatedHolySword,
    Sangue,
    SillyPinkHammer,
    SolarBrand,
    SolarBrand_Glow,
    SolemnLament,
    Threadripper,
    #endregion
    #endregion
    #region Multi
    #region Early
    ChainStrikeJavelin,
    FulgurSpear,
    FulgurSpear_Glow,
    #endregion
    #region Late
    #endregion
    #region Middle
    BoneGunsword,
    #endregion
    #endregion
    #region Ranged
    #region Early
    BeanBurrito,
    BoneFlintlock,
    CrystallineSnapcurve,
    Downpour,
    LooseSawblade,
    ObsidianRound,
    #endregion
    #region Late
    AntiMatterCannon,
    CosmicImplosion,
    FlorescenceRounds,
    GaussBallisticWarheadLauncher,
    HeavenForgedCannon,
    HeavyLaserRifle,
    Lanikea,
    LightripRounds,
    MicroGun,
    SunsplitHorizon,
    TechnicBlitzripper,
    UnparalleledCoalescence,
    UnparalleledCoalescence_Glow,
    #endregion
    #region Middle
    AnvilAndPropane,
    BobmOnAStick,
    BowOfGreekFlames,
    CharringBarrage,
    CopperWireWrappedRock,
    GarciaShotgun,
    Hailfire,
    HallowedGreatbow,
    HemoglobbedCapsule,
    MartianLaserCapsule,
    EbonyNovaBlaster,
    SmartPistolMK6,
    TorrentialStorms,
    TroubledTank,
    #endregion
    #endregion
    #region Summoner
    #region Early
    RampantShields,
    StellarKunai,
    TimberLash,
    #endregion
    #region Late
    Avragen,
    DeepestNadir,
    ScriptureOfTheSuperLoki,
    TidalDeluge,
    #endregion
    #region Middle
    Atorcoppe,
    BatLantern,
    EclipsedDuo,
    Exsanguination,
    HiTechRemote,
    IchorWhip,
    LokiShrine,
    TheTongue,
    WitheredShredder,
    #endregion
    #endregion
    #endregion
    #endregion

    #region NPCs
    #region BossBars
    AsterlinBossbar,
    StygainBossbar,
    #endregion
    #region Bosses
    #region Crater
    Asterlin_BossChecklist,
    Asterlin_Head_Boss,
    Asterlin_Head_BossGlow,
    AsterlinAtlas,
    AsterlinAtlasGlow,
    AsterlinAtlasVentGlow,
    AsterlinFacingForward,
    CyberneticSword,
    FireballArrow,
    GodPiercingDart,
    JudgementHammer,
    LightningNode,
    OverloadedLightDart,
    SeethingRockball,
    #endregion
    #region Stygain
    BloodMoonlet,
    BloodMoonlet_Glow,
    BloodRay,
    CoalescentMass_Head_Boss,
    ExsanguinationLance,
    HemoglobTeleArrow,
    StygainHeart,
    StygainHeart_BossChecklist,
    StygainHeart_Head_Boss,
    WrithingEyeball,
    #endregion
    #region Tidal
    AbyssalCurrent,
    #endregion
    #endregion
    #region Friendly
    CreepOldManBubble,
    CreepyOldMan,
    #endregion
    #region Hostile
    #region Arid
    DuneProwlerAssault,
    DuneProwlerSniper,
    EmptyRound,
    GlassFocusedSniper,
    GlassShell,
    RaggedRifle,
    RifleBullet,
    #endregion
    #region Aurora
    AuroraGuardBestiary,
    AuroraLimbEnd,
    AuroraLimbStart,
    AuroraTurretBarrelGlow,
    AuroraTurretBase,
    AuroraTurretHead,
    AuroraTurretHead_Head_Boss,
    GlacialShell,
    GlacialSpike,
    Glacier,
    #endregion
    #region Fulgur
    FulminationSpirit,
    LightningVolt,
    LightningVolt_Glowmask,
    #endregion
    #region SolarGuardian
    SolarGuardian,
    #endregion
    #endregion
    #region Misc
    GodDummyNPC,
    ParmaJawn,
    Rebar,
    TheGiantSnailFromAncientTimes,
    #endregion
    #endregion

    #region Projectiles
    #region Classless
    #region Early
    BiomePointer,
    BiomePointerBackground,
    ExplosiveStickyDart,
    ProximityDart,
    #endregion
    #region Late
    #region CrossCode
    DiscIceProjectile,
    Overlay,
    Reticle1,
    Reticle2,
    ScarletMeteor,
    ScarletMeteorExplosion,
    ShockLightning,
    SmolBoll,
    VRPFire,
    VRPIce,
    VRPLightning,
    VRPNeutral,
    VRPWave,
    #endregion
    #region Cynosure
    CelestialRendingNeedle,
    OrionsSword,
    UnfathomablePortal,
    UnfathomablePortalGlowmask,
    #endregion
    _500kg,
    AncientRetaliation,
    SharpTie,
    #endregion
    #region Middle
    AuroricParry,
    AuroricShield,
    BoingMyceliumite,
    ExplodingMyceliumite,
    HomingMyceliumite,
    IcyShards,
    PiercingMyceliumite,
    SandBlast,
    TremorSpikeEnd,
    TremorSpikeMiddle,
    #endregion
    #endregion
    #region Magic
    #region Early
    LightningNimbusSparks,
    #endregion
    #region Late
    #region Zenith
    ConcentratedEnergy,
    SeamstressMagic,
    SeamStrike,
    SewingNeedle,
    #endregion
    ArmageddonCircle,
    CometStormHoldout,
    EpidemicCircle,
    #endregion
    #region Middle
    FallingHail,
    HellishLance,
    SanguineLance,
    StarWater,
    StarWaterBreak,
    TheStarsAreAfraid,
    VirulentFlower,
    VirulentProjectile,
    VirulentSeed,
    #endregion
    #endregion
    #region Melee
    #region Early
    MeteorSpawn,
    ObsidianChain,
    ObsidianChainAlt,
    ObsidianMaceProj,
    #endregion
    #region Late
    #region Zenith
    #endregion
    CyberneticSwing,
    EverbladedSwing,
    HeavenForgedSpear,
    KatanaCleave,
    Pigeon,
    ReaperChain,
    #endregion
    #region Middle
    AlucardsSwordThrow,
    AlucardsSwordThrow_Glow,
    CryingEye,
    DecayingCutleryStab,
    Execution,
    HolyCross,
    JusticeIsSplendorW,
    SolemButterflyGrief,
    SolemButterflyLament,
    SolemnLamentProjBlack,
    SolemnLamentProjWhite,
    SplendorIsJusticeW,
    #endregion
    #endregion
    #region Misc
    #endregion
    #region Multi
    #region Early
    ShockJavelin,
    #endregion
    #region Late
    #region Zenith
    #endregion
    #endregion
    #region Middle
    GunSwordHeld,
    #endregion
    #endregion
    #region Pets
    AntFly,
    AntFly_Fly,
    Doohickey,
    GearCat,
    GearCat_Fly,
    JellyfishBro,
    LilBloodSquid,
    #endregion
    #region Ranged
    #region Early
    _9mm,
    BeanFire,
    CalciumBomb,
    CalciumSplinter,
    CrystallineSnapcurveProjLimb1,
    CrystallineSnapcurveProjLimb2,
    DownpourHeld,
    ObsidanShotBreak,
    ObsidianShot,
    RainDrop,
    TheSpores,
    #endregion
    #region Late
    #region Zenith
    DivinityArrow,
    #endregion
    AntiBulletp,
    AntiBulletShell,
    AntiBulletShrapnel,
    CosmicImplosionHoldout,
    Florescence,
    GalaxyShell,
    GaussBallisticWarheadRocket,
    GaussBallisticWarheadRocket_Glow,
    GaussReticle,
    LuminiteRocket,
    TechnicBlitzripperHeat,
    #endregion
    #region Middle
    #region AZ
    Grub,
    GrubShrapnel,
    Maggot,
    Slug,
    TankHeadHoldout,
    TheSwarm,
    #endregion
    BowOfGreekFlamesHeld,
    GreekBombArrow,
    HailfireShell,
    HallowedGreatbowHeld,
    MartianCapsule,
    ShotgunBullet,
    TheAnvil,
    ThePropane,
    TorrentialStormsHeld,
    #endregion
    #endregion
    #region Summoner
    #region Early
    EnchantedShield,
    StellarChain,
    StellarChainExtra,
    TimberWhip,
    #endregion
    #region Late
    #region Zenith
    AvragenMinion,
    FleetDaggers,
    #endregion
    LokiShrinep,
    ThrashedVoid,
    TidalWhip,
    #endregion
    #region Middle
    BatSummon,
    ExsanguinationProj,
    IchorWhipp,
    LazerDrone,
    LunarWhip,
    RemoteHoldout,
    SpiderWhipProjectile,
    TheTongueWhip,
    TongueSegment,
    #endregion
    #endregion
    #region Vanilla
    #region Early
    CrimtaneArrow,
    #endregion
    #region Late
    #endregion
    #region Middle
    DivineArrow,
    KrakenTentacle,
    KrakenTentacleSegment,
    #endregion
    #endregion
    #endregion

    #region Tiles
    AngelsRagePlaced,
    AsterlinRelicPlaced,
    FierceBattlePlaced,
    FlagPolePlaced,
    FrigidGalePlaced,
    GreenBlockPlaced,
    LadikerfosPlaced,
    LockedCyberneticPedestal,
    MechanicalInNature2Placed,
    MechanicalInNaturePlaced,
    MenuMusicPlaced,
    MeteorBlockPlaced,
    MonsterBanner,
    PolarityPlaced,
    RainDancePlaced,
    SereneSatellitePlaced,
    SnailRoarPlaced,
    SpiderMusicPlaced,
    StygainHeartRelicPlaced,
    StygainHeartTrophyPlaced,
    TechnicTransmitterPlaced,
    WereYouFoolinPlaced,
    #endregion

    #region UI
    Background,
    CursorMelee,
    CursorRanged,
    ElementalBalanceBase,
    ElementalBalanceFill,
    ElementalBalanceOutline,
    Fire,
    GodDummyButtons,
    GodDummyUIBackground,
    Ice,
    Index,
    LaserResource,
    LimitBreakerBar,
    LimitBreakerBorder,
    Neutral,
    Shock,
    SmallBar0,
    SmallBar1,
    Wave,
    #endregion

    #region Biomes
    CloudedCraterBackground,
    CloudedCraterIcon,
    #endregion

    #region Gores
    GarciaCartridge,
    GaussBallisticWarheadRocketGore1,
    GaussBallisticWarheadRocketGore2,
    GaussBallisticWarheadRocketGore3,
    LaserDroneGore1,
    LaserDroneGore2,
    LaserDroneGore3,
    #endregion

    #region Buffs
    #region Buff
    DesertsBlessing,
    EternalRest,
    SupremeWaterbreathing,
    WinterHeart,
    #endregion
    #region Debuff
    AshyWater,
    AuroricCooldown,
    CorporealVaporization,
    Curse,
    DentedBySpoon,
    Eclipsed,
    EternalRestCooldown,
    FulminationCooldown,
    HemorrhageTransfer,
    Overheat,
    PlasmaIncineration,
    TheTiesCooldown,
    VoidDebuff,
    Wavebroken,
    #endregion
    #region Summon
    AntBuff,
    AvragenPresence,
    BubbleMan,
    DoohickeyBuff,
    FlockOfRazorShields,
    FlockOfShields,
    GearCatBuff,
    HorrorsBeyondYourComprehension,
    JudgingAsterlin,
    LaserDrones,
    LittleStar,
    LokiBuff,
    MidnightBats,
    SuperLoki,
    #endregion
    BuffTemplate,
    DebuffTemplate,
    WhipDebuff,
    #endregion

    #region Cooldowns
    CooldownAbsolute,
    CooldownAbsoluteOutline,
    CooldownAbsoluteOverlay,
    CooldownAstralDash,
    CooldownAstralDashOutline,
    CooldownAstralDashOverlay,
    CooldownCyberneticParry,
    CooldownCyberneticParryOutline,
    CooldownCyberneticParryOverlay,
    CooldownMimicry,
    CooldownMimicryOutline,
    CooldownMimicryOverlay,
    CooldownMycelium,
    CooldownMyceliumOutline,
    CooldownMyceliumOverlay,
    CooldownPumpkinDash,
    CooldownPumpkinDashOutline,
    CooldownPumpkinDashOverlay,
    CooldownRedMist,
    CooldownRedMistOutline,
    CooldownRedMistOverlay,
    CooldownSkullBomb,
    CooldownSkullBombOutline,
    CooldownSkullBombOverlay,
    CooldownSkullKablooey,
    CooldownKablooeyOutline,
    CooldownKablooeyOverlay,
    CooldownTremor,
    CooldownTremorOutline,
    CooldownTremorOverlay,
    #endregion

    #region Particles
    BaseRarityGlow,
    BaseRaritySparkleTexture,
    DropletTexture,
    BloodParticle,
    BloodParticle2,
    CartoonAngerParticle,
    CloudParticle,
    CritSpark,
    CrossCodeBoll,
    CrossCodeHit,
    DetailedBlast,
    DetailedBlast2,
    DustParticle,
    Flames,
    HeavySmoke,
    MediumSmoke,
    Menacing,
    MistParticle,
    SmokeParticle,
    Snowflake,
    Sparkle,
    Star,
    StarLong,
    TechyHolosquare,
    ThunderBolt,
    TileGlowmask,
    #endregion
}

public enum AdditionsSound
{
    #region Ambience
    bigwind,
    CosmicEcho,
    CreepyAir,
    DarkHumm,
    heartbeat,
    Raining,
    waterfall,
    #endregion

    #region Base
    AuroraKABLOOEY,
    AuroraRise,
    AuroraTink1,
    AuroraTink2,
    AuroraTink3,
    Deep,
    DeepHit,
    Healing,
    Heavenly,
    HeavyHit,
    HeavyWhooshShort,
    Icicles,
    Laser,
    Laser3,
    Laser4,
    Laser6,
    LaserHum,
    LaserShift,
    LaserTwo,
    MagicHit,
    MagicRockMine,
    MagicStrike,
    MagicSwing,
    MagicSwoosh,
    MagicWater,
    MetalHit1,
    MetalHit2,
    MetalHit3,
    MetallicImpact,
    MetalStrike,
    MeteorWhoosh,
    MimicryLand,
    MimicrySwing,
    RaptureCharge,
    RockBreak,
    Shattering,
    smallswing,
    SmithHammerHeavy,
    SolemnB,
    SolemnW,
    SuperImpact,
    Tick,
    TickLoud,
    Trees,
    WarningBeep,
    WaterHit,
    WaterMagic,
    WaterSpell,
    WeaponFail,
    Whoosh,
    WibtorNUKE,
    #endregion

    #region Blast
    Garciaboom,
    GaussBoom,
    GaussBoomLittle,
    GaussWeaponFire,
    HeavyClash,
    HeavyLaserBlast,
    LargeSniperFire,
    LargeWeaponFire,
    LargeWeaponFireDifferent,
    MediumExplosion,
    MetalBoom,
    MeteorImpact,
    MomentOfCreation,
    Rapture,
    SizableExplosion,
    SniperShot,
    #endregion

    #region Calamity
    AdrenalineMajorLoss,
    AstrumDeusLaser,
    BeegBell,
    BloodPactCrit,
    BrimstoneBigShoot,
    CeaselessVoidDeath,
    ExoPlasmaExplosion2,
    LegStomp,
    #endregion

    #region CrossCode
    AsterlinHit,
    blackHoleSuck,
    ColdBallThrow,
    ColdBallThrowCharged,
    ColdBounce,
    ColdHitBig,
    ColdHitMassive,
    ColdHitMedium,
    ColdHitSmall,
    ColdPunch,
    ColdSweep,
    ColdSweepMassive,
    crosscodeBallDie,
    crosscodeExplosion,
    GunLoop,
    HeatBallThrow,
    HeatBallThrowCharged,
    HeatBounce,
    HeatHitBig,
    HeatHitMedium,
    HeatHitSmall,
    HeatMeteorBoom,
    HeatMeteorFall,
    HeatSweep,
    HeatSweepMassive,
    HeatTail,
    holy,
    holyFloating,
    holyLoop,
    metalSlam,
    NeutralBallHitBig,
    NeutralBallHitMedium,
    NeutralBallHitSmall,
    NeutralBallThrow,
    NeutralBallThrowCharged,
    NeutralBounce,
    NeutralHitBig,
    NeutralHitMedium,
    NeutralSweep,
    NeutralSweepMassive,
    pierce,
    planetImpact,
    ShockBallThrow,
    ShockBallThrowCharged,
    ShockBounce,
    ShockHitBig,
    ShockHitMedium,
    ShockHitSmall,
    ShockSweep,
    ShockSweepMassive,
    spearCharge,
    spearLaser,
    sunAura,
    SuperCharge,
    WaveBallThrow,
    WaveBallThrowCharged,
    WaveBounce,
    WaveHitBig,
    WaveHitMedium,
    WaveHitSmall,
    WaveSweep,
    WaveSweepMassive,
    #endregion

    #region Electric
    ElectricalPow,
    ElectricalPowBoom,
    ElectricCast,
    electrichit,
    LightningExplosion,
    LightningStrike,
    PowerDown,
    StaticHum,
    StaticRip,
    ThunderExplosive,
    ThunderImpact,
    ThunderImpactHeavy,
    ThunderLaser,
    #endregion

    #region Fire
    BallCreate,
    BallFire,
    Fireball,
    FireballShort,
    FireBeamEnd,
    FireBeamLoop,
    FireBeamStart,
    FireBig,
    FireBreathe,
    FireBreathe4,
    FireBreathEnd,
    FireBreathStart,
    FireGout,
    FireMagic,
    FireSwoosh,
    FireWhoosh1,
    FireWhoosh2,
    HeavyFireLoop,
    #endregion

    #region Misc.
    Afraid,
    BlueBerryBUFFINS,
    ClairDeLune,
    PETER,
    thrall,
    #endregion

    #region Music
    clairdelune,
    FrigidGale,
    Infinite,
    Ladikerfos,
    MechanicalInNature,
    MechanicalInNature2,
    Perdition,
    Protostar,
    RainDance,
    sickest_beat_ever,
    Spider,
    SRank,
    wereyoufoolin,
    #endregion

    #region Ori
    etherealBlazeStart,
    etherealBounce,
    etherealBounceSmall,
    etherealChargeBoom,
    etherealChargeBoom2,
    etherealChargeStart,
    etherealHit1,
    etherealHit2,
    etherealHit3,
    etherealHit4,
    etherealHit5,
    etherealHitBoom1,
    etherealHitBoom2,
    etherealHitCrunch,
    etherealLoose,
    etherealMagicBlast,
    etherealNuhUh,
    etherealRelease,
    etherealRelease2,
    etherealReleaseA,
    etherealSharpImpact,
    etherealSharpImpactA,
    etherealSharpImpactB,
    etherealSharpImpactC,
    etherealSlam,
    etherealSmallExplode,
    etherealSmallHit,
    etherealSmash,
    etherealSmash2,
    etherealSplit,
    etherealSwordAttackBasic1,
    etherealSwordAttackBasic2,
    etherealSwordAttackBasic3,
    etherealSwordSwingA,
    etherealSwordSwingB,
    etherealSwordSwingC,
    etherealSwordSwoosh,
    etherealThrow,
    #endregion

    #region RoR
    banditReload,
    banditShot1A,
    banditShot1B,
    banditShot2A,
    banditShot2B,
    BigSwing,
    BigSwing2,
    charExplo,
    charShot,
    commandoBlast1,
    commandoBlast2,
    explo01,
    explo04,
    FireImpact,
    FireImpact2,
    HeavySwordSwing,
    ImpSmash,
    MediumSwing,
    MediumSwing2,
    SwordSlice,
    SwordSliceShort,
    #endregion

    #region SSBU
    BreakerBeam,
    BreakerCapped,
    BreakerCharge,
    BreakerChargeFull,
    BreakerHit1,
    BreakerHit2,
    BreakerStorm,
    BreakerSwing,
    BreakerSwingSpecial,
    BreakerUp,
    BreakerUpHit,
    BraveAttackAirN01,
    BraveAttackDash02,
    BraveAttackDash03,
    BraveBigFireLoop,
    BraveSmashH01,
    BraveDashStart,
    BraveEnergy,
    BraveEnergy2,
    BraveHeavyFireHit,
    BraveIceSlash,
    BraveMediumFireLoop,
    BraveShieldGuard,
    BraveSmallFireLoop,
    BraveSmashS02,
    BraveSpecial1A,
    BraveSpecial1B,
    BraveSpecial1C,
    BraveSpecial2A,
    BraveSpecial2B,
    BraveSwingLarge,
    BraveSwingMedium,
    BraveThrow,
    IkePullout,
    IkeStab,
    IkeFinal,
    IkeSpecial1,
    IkeSpecial2,
    IkeSpecial3,
    IkeSpecial4,
    IkeSpecial5,
    IkeSpecial1A,
    IkeSpecial1B,
    IkeSwingMedium,
    IkeSwingSmall,
    IkeSwordGroundHit,
    IkeMaster1,
    IkeMaster2,
    IkeMaster3,
    IkeMaster4,
    IkeMaster5,
    RoySmash,
    RoySpecial1,
    RoySpecial2,
    RoySwordIn,
    RoySwordOut,
    SnakeHeavyHit,
    SnakeGrenadePull,
    SnakeGrenadeThrow,
    SnakeGrenadeBoom,
    SnakeRocket,
    SnakeRocketOut,
    SnakeKablooey,
    #endregion

    #region Ultrakill
    AsterlinChange,
    BlackHoleExplosion,
    BlackHoleLoop,
    chainsawThrown,
    ElectricityContinuous,
    explosion_large_08,
    GabrielSwing,
    GabrielTelegraph,
    GabrielTeleport,
    GabrielWeaponBreak,
    harpoonStop,
    PipIdle,
    PlasticHit,
    SteamRelease,
    TensionDrone,
    UIStart,
    VirtueAttack,
    VirtueCharge,
    #endregion
}

#endregion