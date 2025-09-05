using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Consumable.BossBags;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Placeable.Base;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

/// Basic initialization for Asterlin
public partial class Asterlin
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Asterlin_BossChecklist);
    public override string BossHeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.Asterlin_Head_Boss);
    public override void SetStaticDefaults()
    {
        NPCID.Sets.TrailingMode[NPC.type] = 3;
        NPCID.Sets.TrailCacheLength[NPC.type] = 30;

        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Slow & BuffID.Webbed & BuffID.Confused] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.UsesNewTargetting[Type] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            CustomTexturePath = AssetRegistry.GetTexturePath(AdditionsTexture.Asterlin_BossChecklist),
            PortraitScale = 0.6f,
            PortraitPositionYOverride = -40
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
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
        NPC.SetLifeMaxByMode(1300000 / 3, 2800000 / 3, 4500000 / 3);
        NPC.damage = 300;
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
        NPC.HitSound = AssetRegistry.GetSound(AdditionsSound.AsterlinHit) with { Volume = 1f, PitchVariance = .2f, PitchRange = new(-.2f, 0f) };
        NPC.DeathSound = null;
        NPC.value = Item.buyPrice(50, 0, 0, 0) / 5;
        NPC.netAlways = true;

        if (!Main.dedServ)
        {
            Music = MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature));
        }

        NPC.scale = NPC.Opacity = ZPosition = 1f;
        Initialize();
    }

    public override void Load()
    {
        LoadDialogue();
        MusicBoxAutoloader.Create(Mod, AssetRegistry.AutoloadedPrefix + "MechanicalInNature2", AssetRegistry.GetMusicPath(AdditionsSound.MechanicalInNature2), out _, out _);
    }

    public override void Unload()
    {
        UnloadDialogue();
    }

    public void Initialize()
    {
        leftArm = new JointChain(
                NPC.Center,
                (LeftAngledBackLimbRect.Height, null), // Back limb
                (LeftAngledForeLimbRect.Height, null), // Fore limb
                (LeftAngledHandRect.Height, null) // Hand
            );

        rightArm = new JointChain(
                NPC.Center,
                (RightAngledBackLimbRect.Height, null), // Back limb
                (RightAngledForeLimbRect.Height, null), // Fore limb
                (RightAngledHandRect.Height, null) // Hand
            );
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
        NPC.lifeMax = (int)(NPC.lifeMax * bossAdjustment / (Main.masterMode ? 3f : 2f));
    }

    public override void BossLoot(ref int potionType) => potionType = ItemID.SuperHealingPotion;

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<TreasureBoxAsterlin>()));
        //npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<JellyfishSnack>()));
        npcLoot.Add(ModContent.ItemType<LockedCyberneticSword>(), 1);
        //itemLoot.Add(ModContent.ItemType<FerrymansToken>(), 1);

        LeadingConditionRule normalOnly = npcLoot.DefineNormalOnlyDropSet();
        int[] weapons =
        [
            ModContent.ItemType<CyberneticRocketGauntlets>(),
            ModContent.ItemType<TechnicBlitzripper>(),
            ModContent.ItemType<LightripRounds>(),
            ModContent.ItemType<TesselesticMeltdown>(),
            ModContent.ItemType<LivingStarFlare>(),
        ];
        normalOnly.Add(DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, weapons));
    }
}