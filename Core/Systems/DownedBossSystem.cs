using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TheExtraordinaryAdditions.Core.Systems;

public sealed class DownedBossSystem : ModSystem
{
    public const string KeyPrefix = "downedBoss:";

    public const string StygainKey = "Stygain";
    public const string AbysslonKey = "Abysslon";
    public const string AsterlinKey = "Asterlin";

    public static DownedBossSystem Instance => ModContent.GetInstance<DownedBossSystem>();

    public bool StygainDowned
    {
        get => downedBoss[StygainKey];
        set => downedBoss[StygainKey] = value;
    }
    public bool AbysslonDowned
    {
        get => downedBoss[AbysslonKey];
        set => downedBoss[AbysslonKey] = value;
    }
    public bool AsterlinDowned
    {
        get => downedBoss[AsterlinKey];
        set => downedBoss[AsterlinKey] = value;
    }

    private readonly Dictionary<string, bool> downedBoss = new()
    {
        {
            StygainKey,
            false
        },
        {
            AbysslonKey,
            false
        },
        {
            AsterlinKey,
            false
        }
    };

    public override void SaveWorldData(TagCompound tag)
    {
        foreach (string entry in downedBoss.Keys)
        {
            tag[KeyPrefix + entry] = downedBoss[entry];
        }
    }
    public override void LoadWorldData(TagCompound tag)
    {
        foreach (string entry in downedBoss.Keys)
        {
            downedBoss[entry] = tag.GetBool(KeyPrefix + entry);
        }
    }
}
