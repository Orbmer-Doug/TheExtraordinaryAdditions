using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Autoloaders;
using TheExtraordinaryAdditions.Content.Items.Consumable.BossBags;
using TheExtraordinaryAdditions.Content.Items.Equipable.Pets;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Content.NPCs.BossBars;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

// Basic initialization for Asterlin
public partial class Asterlin
{
    public override string Texture => AssetRegistry.GennedTextures.Asterlin_BossChecklist.Path;
    public override string BossHeadTexture => AssetRegistry.GennedTextures.Asterlin_Head_Boss.Path;

    public override void SetStaticDefaults()
    {
        NPCID.Sets.TrailingMode[NPC.type] = 3;
        NPCID.Sets.TrailCacheLength[NPC.type] = 30;

        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Slow & BuffID.Webbed & BuffID.Confused] = true;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.UsesNewTargetting[Type] = true;
        NPCID.Sets.DoesntDespawnToInactivityAndCountsNPCSlots[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            CustomTexturePath = AssetRegistry.GennedTextures.Asterlin_BossChecklist.Path,
            PortraitScale = .4f,
            PortraitPositionYOverride = 100
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange(
        [
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
            new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
        ]);
    }

    public override void SetDefaults()
    {
        NPC.lifeMax = 1_500_000;
        NPC.damage = 335;
        NPC.defense = 150;
        NPC.width = 128;
        NPC.height = 278;
        NPC.npcSlots = 100f;
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.knockBackResist = 0f;
        NPC.canGhostHeal = false;
        NPC.boss = true;
        NPC.noGravity = false;
        NPC.noTileCollide = false;
        NPC.HitSound = AssetRegistry.GennedSounds.AsterlinHit with
        {
            Volume = 1f, PitchVariance = .2f, PitchRange = new(-.2f, 0f)
        };
        NPC.DeathSound = null;
        NPC.value = Item.buyPrice(50) / 5f;
        NPC.BossBar = ModContent.GetInstance<AsterlinBossbar>();
        NPC.netAlways = true;

        if (!Main.dedServ)
        {
            Music = MusicLoader.GetMusicSlot(Mod, AssetRegistry.GennedSounds.Music.MechanicalInNature.SoundPath);
        }

        NPC.scale = NPC.Opacity = ZPosition = 1f;
        InitializeGraphics();
    }

    public const string LocalizedKey = "NPCs.Asterlin.";

    public static int MaskID { get; private set; }

    public static int RelicID { get; private set; }

    public override void Load()
    {
        RelicAutoloader.Create(Mod, AssetRegistry.GennedTextures.AsterlinRelic.Path,
            AssetRegistry.GennedTextures.AsterlinRelicPlaced.Path, out int id);
        RelicID = id;
        MaskID = MaskAutoloader.Create(Mod, AssetRegistry.GennedTextures.AsterlinMask.Path, false);

        On_NPC.UpdateNPC += MoreUpdates;
        LoadGraphics();
    }

    public override void Unload()
    {
        On_NPC.UpdateNPC -= MoreUpdates;
        UnloadGraphics();
    }

    // For any high speed attacks we dont want the hitbox to skip over players
    public int ExtraUpdates;
    public int NumUpdates;

    private static void MoreUpdates(On_NPC.orig_UpdateNPC orig, NPC self, int i)
    {
        if (self.type == ModContent.NPCType<Asterlin>())
        {
            Asterlin aster = self.As<Asterlin>();
            aster.NumUpdates = aster.ExtraUpdates;
            while (aster.NumUpdates >= 0)
            {
                aster.NumUpdates--;
                orig(self, i);
            }
        }
        else
            orig(self, i);
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

    public override void BossHeadSlot(ref int index)
    {
        // Make the head icon disappear if invisible
        if (NPC.Opacity <= 0.45f)
            index = -1;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        // Unfortunately, theres no way to check the total amount of players between both worlds
        // And due to the shenanigans described in Asterlin.AbsorbingEnergy, it wont capture the correct amount of players
        NPC.lifeMax = (int) (NPC.lifeMax * 0.8f * balance * bossAdjustment);
    }

//TODO
    public override void BossLoot(ref int potionType) => potionType = ItemID.AaronsBreastplate;

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<TreasureBoxAsterlin>()));

        LeadingConditionRule normalRule = npcLoot.DefineNormalOnlyDropSet();
        int[] itemIDs =
        [
            ModContent.ItemType<CyberneticRocketGauntlets>(),
            ModContent.ItemType<TechnicBlitzripper>(),
            ModContent.ItemType<LightripRounds>(),
            ModContent.ItemType<TesselesticMeltdown>(),
            ModContent.ItemType<LivingStarFlare>(),
        ];
        normalRule.Add(DropHelper.PityStyle(DropHelper.NormalWeaponDropRateFraction, itemIDs));
        normalRule.Add(MaskID, 10);

        npcLoot.Add(ModContent.ItemType<LockedCyberneticSword>(), 10);
        npcLoot.DefineConditionalDropSet(new Conditions.IsMasterMode()).Add(RelicID);
        npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<TVRemote>()));
    }
}
