using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Pets;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Summon;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.CrossCompatibility;

internal class WeakReferenceSupport
{
    public static void Setup()
    {
        BossChecklistSupport();
    }

    private static void AddBoss(Mod bossChecklist, Mod hostMod, string name, float difficulty, Func<bool> downed, object npcTypes, Dictionary<string, object> extraInfo)
    {
        bossChecklist.Call(["LogBoss", hostMod, name, difficulty, downed, npcTypes, extraInfo]);
    }

    private static void AddMiniBoss(Mod bossChecklist, Mod hostMod, string name, float difficulty, Func<bool> downed, int npcType, Dictionary<string, object> extraInfo)
    {
        bossChecklist.Call(["LogMiniBoss", hostMod, name, difficulty, downed, npcType, extraInfo]);
    }

    private static void AddEvent(Mod bossChecklist, Mod hostMod, string name, float difficulty, Func<bool> downed, List<int> npcTypes, Dictionary<string, object> extraInfo)
    {
        bossChecklist.Call(["LogEvent", hostMod, name, difficulty, downed, npcTypes, extraInfo]);
    }

    private static LocalizedText GetDisplayName(string entryName)
    {
        return GetText("BossChecklistIntegration." + entryName + ".EntryName");
    }

    private static LocalizedText GetSpawnInfo(string entryName)
    {
        return GetText("BossChecklistIntegration." + entryName + ".SpawnInfo");
    }

    private static LocalizedText GetDespawnMessage(string entryName)
    {
        return GetText("BossChecklistIntegration." + entryName + ".DespawnMessage");
    }

    private static void BossChecklistSupport()
    {
        AdditionsMain additions = ModContent.GetInstance<AdditionsMain>();
        Mod bossChecklist = additions.bossChecklist;
        if (bossChecklist != null)
        {
            AddAdditionsBosses(bossChecklist, (Mod)(object)additions);
        }
    }

    private static void AddAdditionsBosses(Mod bossChecklist, Mod additions)
    {
        #region Stygian
        string stygianName = "StygainHeart";
        List<int> boss =
    [
        ModContent.NPCType<StygainHeart>(),
    ];
        List<int> collection31 =
    [
        ModContent.ItemType<CrimsonCalamari>(),
    ];
        Action<SpriteBatch, Rectangle, Color> portrait11 = delegate (SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D value11 = AssetRegistry.GetTexture(AdditionsTexture.StygainHeart_BossChecklist);
            Vector2 val11 = new Vector2(rect.Center.X - value11.Width / 2, rect.Center.Y - value11.Height / 2);
            sb.Draw(value11, val11, color);
        };
        AddBoss(bossChecklist, additions, stygianName, 16.6f, () => DownedBossSystem.Instance.StygainDowned, boss, new Dictionary<string, object>
        {
            ["displayName"] = GetDisplayName(stygianName),
            ["spawnInfo"] = GetSpawnInfo(stygianName),
            ["despawnMessage"] = GetDespawnMessage(stygianName),
            ["spawnItems"] = ModContent.ItemType<CrimsonCarvedBeetle>(),
            ["collectibles"] = collection31,
            ["customPortrait"] = portrait11
        });
        #endregion Stygian

        #region Asterlin
        string asterName = "Asterlin";
        List<int> aster =
    [
        ModContent.NPCType<Asterlin>(),
    ];
        List<int> asterlinCollection =
        [
        ModContent.ItemType<LockedCyberneticSword>(),
    ];
        Action<SpriteBatch, Rectangle, Color> asterlinPortrait = delegate (SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D value11 = AssetRegistry.GetTexture(AdditionsTexture.Asterlin_BossChecklist);
            Vector2 val11 = new(rect.Center.X - value11.Width / 2, rect.Center.Y - value11.Height / 2);
            sb.Draw(value11, val11, color);
        };
        AddBoss(bossChecklist, additions, asterName, 21.5f, () => DownedBossSystem.Instance.AsterlinDowned, aster, new Dictionary<string, object>
        {
            ["displayName"] = GetDisplayName(asterName),
            ["spawnInfo"] = GetSpawnInfo(asterName),
            ["despawnMessage"] = GetDespawnMessage(asterName),
            ["spawnItems"] = ModContent.ItemType<WorldShatteredFragment>(),
            ["collectibles"] = asterlinCollection,
            ["customPortrait"] = asterlinPortrait
        });
        #endregion Asterlin
    }
}
