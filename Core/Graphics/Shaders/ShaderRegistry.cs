using Terraria;

namespace TheExtraordinaryAdditions.Core.Graphics.Shaders;

/// <summary>
/// Simple access to all shaders present in the autoloaded effects folder
/// <br></br>
/// <i>All shaders and filters should added here</i>
/// </summary>
/// TODO: summaries that describe params in shader file?
public static class ShaderRegistry
{
    #region Filter Shorthands
    public static ManagedScreenShader GenediesDistortion => AssetRegistry.GetFilter("GenediesDistortion");
    public static ManagedScreenShader BlackHole => AssetRegistry.GetFilter("BlackHole");
    public static ManagedScreenShader GaussianBlur => AssetRegistry.GetFilter("GaussianBlur");
    public static ManagedScreenShader ScreenShakeShader => AssetRegistry.GetFilter("ScreenShakeShader");
    public static ManagedScreenShader ScreenShakeShader2 => AssetRegistry.GetFilter("ScreenShakeShader2");
    public static ManagedScreenShader SwirlDistortion => AssetRegistry.GetFilter("SwirlDistortion");

    #endregion Filter Shorthands

    #region Shader Shorthands
    public static ManagedShader PixelatedSightLine => AssetRegistry.GetShader("PixelatedSightLine");
    public static ManagedShader SpreadTelegraph => AssetRegistry.GetShader("SpreadTelegraph");
    public static ManagedShader Swing => AssetRegistry.GetShader("Swing");
    public static ManagedShader TexOverlay => AssetRegistry.GetShader("TexOverlay");
    public static ManagedShader Forcefield => AssetRegistry.GetShader("Forcefield");
    public static ManagedShader ForcefieldLimited => AssetRegistry.GetShader("ForcefieldLimited");
    public static ManagedShader ForcefieldUnique => AssetRegistry.GetShader("ForcefieldUnique");
    public static ManagedShader AdditiveFusableParticleEdgeShader => AssetRegistry.GetShader("AdditiveFusableParticleEdgeShader");
    public static ManagedShader AppearShader => AssetRegistry.GetShader("AppearShader");
    public static ManagedShader AsterlinDeathrayShader => AssetRegistry.GetShader("AsterlinDeathrayShader");
    public static ManagedShader BaseLaserShader => AssetRegistry.GetShader("BaseLaserShader");
    public static ManagedShader CrunchyLaserShader => AssetRegistry.GetShader("CrunchyLaserShader");
    public static ManagedShader BasicTint => AssetRegistry.GetShader("BasicTint");
    public static ManagedShader BorderNoise => AssetRegistry.GetShader("BorderNoise");
    public static ManagedShader GlitchDisplacement => AssetRegistry.GetShader("GlitchDisplacement");
    public static ManagedShader CircularAoETelegraph => AssetRegistry.GetShader("CircularAoETelegraph");
    public static ManagedShader InverseCircularAOE => AssetRegistry.GetShader("InverseCircularAOE");
    public static ManagedShader EnlightenedBeam => AssetRegistry.GetShader("EnlightenedBeam");
    public static ManagedShader MagicRing => AssetRegistry.GetShader("MagicRing");
    public static ManagedShader FireballShader => AssetRegistry.GetShader("FireballShader");
    public static ManagedShader FireTrail => AssetRegistry.GetShader("FireTrail");
    public static ManagedShader BloodBeacon => AssetRegistry.GetShader("BloodBeaconShader");
    public static ManagedShader DissipatedGlowTrail => AssetRegistry.GetShader("DissipatedGlowTrail");
    public static ManagedShader FlameTrail => AssetRegistry.GetShader("FlameTrail");
    public static ManagedShader FogShader => AssetRegistry.GetShader("FogShader");
    public static ManagedShader GammaRayShader => AssetRegistry.GetShader("GammaRay");
    public static ManagedShader LightningArcShader => AssetRegistry.GetShader("LightningArcShader");
    public static ManagedShader LightShockwave => AssetRegistry.GetShader("LightShockwave");
    public static ManagedShader MagicCircleShader => AssetRegistry.GetShader("MagicCircleShader");
    public static ManagedShader MagicSphere => AssetRegistry.GetShader("MagicSphere");
    public static ManagedShader EdgeDetectionShader => AssetRegistry.GetShader("EdgeDetectionShader");
    public static ManagedShader NoiseDisplacement => AssetRegistry.GetShader("NoiseDisplacement");
    public static ManagedShader OceanLayer => AssetRegistry.GetShader("OceanLayer");
    public static ManagedShader PierceTrailShader => AssetRegistry.GetShader("PierceTrailShader");
    public static ManagedShader PixelationEffect => AssetRegistry.GetShader("PixelationEffect");
    public static ManagedShader PortalShader => AssetRegistry.GetShader("PortalShader");
    public static ManagedShader PrimitiveBloomShader => AssetRegistry.GetShader("PrimitiveBloomShader");
    public static ManagedShader HeatDistortionShader => AssetRegistry.GetShader("HeatDistortionShader");
    public static ManagedShader RealityTearShader => AssetRegistry.GetShader("RealityTearShader");
    public static ManagedShader ShockwaveShader => AssetRegistry.GetShader("ShockwaveShader");
    public static ManagedShader SideStreakTrail => AssetRegistry.GetShader("SideStreakTrail");
    public static ManagedShader SmoothFlame => AssetRegistry.GetShader("SmoothFlame");
    public static ManagedShader StarShader => AssetRegistry.GetShader("StarShader");
    public static ManagedShader SpecialLightningTrail => AssetRegistry.GetShader("SpecialLightningTrail");
    public static ManagedShader Supernova => AssetRegistry.GetShader("Supernova");
    public static ManagedShader SwingFaded => AssetRegistry.GetShader("SwingFaded");
    public static ManagedShader SwingShader => AssetRegistry.GetShader("SwingShader");
    public static ManagedShader SwingShaderIntense => AssetRegistry.GetShader("SwingShaderIntense");
    public static ManagedShader SwordRipShader => AssetRegistry.GetShader("SwordRipShader");
    public static ManagedShader TexClip => AssetRegistry.GetShader("IntersectionFilter");
    public static ManagedShader StandardPrimitiveShader => AssetRegistry.GetShader("StandardPrimitiveShader");
    public static ManagedShader ViscousAfterimageShader => AssetRegistry.GetShader("ViscousAfterimageShader");
    public static ManagedShader ViscousVoidShader => AssetRegistry.GetShader("ViscousVoidShader");
    public static ManagedShader VortexShader => AssetRegistry.GetShader("VortexShader");
    public static ManagedShader WaterCurrent => AssetRegistry.GetShader("WaterCurrent");
    public static ManagedShader FadedStreak => AssetRegistry.GetShader("FadedStreak");

    #endregion Shader Shorthands
}