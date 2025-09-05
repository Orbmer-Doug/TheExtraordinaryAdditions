using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.World.Subworlds;

namespace TheExtraordinaryAdditions.Content.Biomes;

public class CloudedCraterBiome : ModBiome
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

    public override string BestiaryIcon => AssetRegistry.GetTexturePath(AdditionsTexture.CloudedCraterIcon);

    public override string BackgroundPath => AssetRegistry.GetTexturePath(AdditionsTexture.CloudedCraterBackground);

    public override string MapBackground => AssetRegistry.GetTexturePath(AdditionsTexture.CloudedCraterBackground);

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Audio/Music/MechanicalInNature");

    public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<CloudedCrater>();

    public override float GetWeight(Player player) => 0.96f;
}
