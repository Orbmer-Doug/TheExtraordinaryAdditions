using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Pets;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Summon;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.Systems;

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
            AddAdditionsBosses(bossChecklist, (Mod)(object)additions);
    }

    private static void AddAdditionsBosses(Mod bossChecklist, Mod additions)
    {
        #region Stygian
        string stygainName = "StygainHeart";
        List<int> stygainType =
        [
            ModContent.NPCType<StygainHeart>(),
        ];
        List<int> stygainCollection =
        [
            ModContent.ItemType<CrimsonCalamari>(),
        ];
        Action<SpriteBatch, Rectangle, Color> stygainPortrait = delegate (SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.StygainHeart_BossChecklist);
            Vector2 pos = new Vector2(rect.Center.X - tex.Width / 2, rect.Center.Y - tex.Height / 2);
            sb.Draw(tex, pos + Vector2.UnitY * MathHelper.Lerp(-20f, 20f, Sin01(Main.GlobalTimeWrappedHourly)), color);
        };
        AddBoss(bossChecklist, additions, stygainName, 16.6f, () => BossDownedSaveSystem.HasDefeated<StygainHeart>(), stygainType, new Dictionary<string, object>
        {
            ["displayName"] = GetDisplayName(stygainName),
            ["spawnInfo"] = GetSpawnInfo(stygainName),
            ["despawnMessage"] = GetDespawnMessage(stygainName),
            ["spawnItems"] = ModContent.ItemType<CrimsonCarvedBeetle>(),
            ["collectibles"] = stygainCollection,
            ["customPortrait"] = stygainPortrait
        });
        #endregion Stygian

        #region Asterlin
        string asterlinName = "Asterlin";
        List<int> asterlinType =
        [
            ModContent.NPCType<Asterlin>(),
        ];
        List<int> asterlinCollection =
        [
            ModContent.ItemType<LockedCyberneticSword>(),
            ];
        Action<SpriteBatch, Rectangle, Color> asterlinPortrait = delegate (SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Asterlin_BossChecklist);
            Vector2 pos = new(rect.Center.X - tex.Width / 2, rect.Center.Y - tex.Height / 2);
            sb.Draw(tex, pos, color);
        };
        AddBoss(bossChecklist, additions, asterlinName, 21.5f, () => BossDownedSaveSystem.HasDefeated<Asterlin>(), asterlinType, new Dictionary<string, object>
        {
            ["displayName"] = GetDisplayName(asterlinName),
            ["spawnInfo"] = GetSpawnInfo(asterlinName),
            ["despawnMessage"] = GetDespawnMessage(asterlinName),
            ["spawnItems"] = ModContent.ItemType<TechnicTransmitter>(),
            ["collectibles"] = asterlinCollection,
            ["customPortrait"] = asterlinPortrait
        });
        #endregion Asterlin

        #region Aurora
        string auroraName = "AuroraGuard";
        List<int> auroraCollection =
        [
            
        ];
        Action<SpriteBatch, Rectangle, Color> auroraPortrait = delegate (SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.AuroraGuardBestiary);
            Vector2 pos = new Vector2(rect.Center.X - tex.Width / 2, rect.Center.Y - tex.Height / 2);
            sb.Draw(tex, pos, color);
        };
        AddMiniBoss(bossChecklist, additions, auroraName, 7.9f, () => BossDownedSaveSystem.HasDefeated<AuroraGuard>(), ModContent.NPCType<AuroraGuard>(), new Dictionary<string, object>
        {
            ["displayName"] = GetDisplayName(auroraName),
            ["spawnInfo"] = GetSpawnInfo(auroraName),
            ["despawnMessage"] = GetDespawnMessage(auroraName),
            ["spawnItems"] = ItemID.FrostCore,
            ["collectibles"] = auroraCollection,
            ["customPortrait"] = auroraPortrait
        });
        #endregion
    }
}
