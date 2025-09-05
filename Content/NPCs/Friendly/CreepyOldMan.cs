using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Consumable;
using TheExtraordinaryAdditions.Content.Items.Novelty;
using TheExtraordinaryAdditions.Content.Items.Weapons.Classless;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Friendly;

public class CreepyOldMan : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CreepyOldMan);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 25;

        NPCID.Sets.ExtraFramesCount[Type] = 9;
        NPCID.Sets.AttackFrameCount[Type] = 4;
        NPCID.Sets.DangerDetectRange[Type] = 600;
        NPCID.Sets.AttackType[Type] = 1;
        NPCID.Sets.AttackTime[Type] = 60;
        NPCID.Sets.AttackAverageChance[Type] = 30;
        NPCID.Sets.HatOffsetY[Type] = 4;
        NPCID.Sets.ShimmerTownTransform[NPC.type] = false;

        // This sets entry is the most important part of this NPC. Since it is true, it tells the game that we want this NPC to act like a town NPC without ACTUALLY being one.
        // Essentially making him skeleton merchant
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
        NPCID.Sets.NoTownNPCHappiness[Type] = true;
        NPCID.Sets.SpawnsWithCustomName[Type] = true;

        // This makes it when the NPC is in the world, other NPCs will "talk about him"
        NPCID.Sets.FaceEmote[Type] = ModContent.EmoteBubbleType<OldManBubble>();
        NPCID.Sets.AllowDoorInteraction[Type] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
        {
            Velocity = 1f,
            Direction = -1
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
    }

    public override void SetDefaults()
    {
        NPC.friendly = true;
        NPC.scale = 1.5f;
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.damage = 10;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.9f;

        AnimationType = NPCID.Guide;
    }

    public override bool CanChat() => true;

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary")),
        ]);
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        if (NPC.life > 0)
            return;

        if (NPC.GivenName == this.GetLocalizedValue("Name8"))
        {
            for (int i = 0; i < 30; i++)
            {
                ParticleRegistry.SpawnGlowParticle(NPC.RandAreaInEntity(), Vector2.UnitY * -Main.rand.NextFloat(4f, 10f), 20, .5f, Color.White, 1f, true);
            }
        }
        else
        {
            int headGore = Mod.Find<ModGore>($"{Name}_Gore_Head").Type;
            int armGore = Mod.Find<ModGore>($"{Name}_Gore_Arm").Type;
            int legGore = Mod.Find<ModGore>($"{Name}_Gore_Leg").Type;

            // Spawn the gores
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, headGore, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 20), NPC.velocity, armGore);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 20), NPC.velocity, armGore);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 34), NPC.velocity, legGore);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 34), NPC.velocity, legGore);
        }
    }

    public override List<string> SetNPCNameList()
    {
        List<string> list = [];
        for (int i = 1; i < 10; i++)
            list.Add(this.GetLocalizedValue($"Name{i}"));

        return list;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (spawnInfo.Player.ZoneDirtLayerHeight && spawnInfo.Player.inventory.Any(item => item.type >= ItemRarityID.Yellow) && NPC.downedPlantBoss)
            return 0.11f;
        
        return 0f;
    }

    public override string GetChat()
    {
        WeightedRandom<string> chat = new();

        for (int i = 1; i < 7; i++)
            chat.Add(this.GetLocalizedValue($"Chat{i}"));

        return chat;
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = Language.GetTextValue("LegacyInterface.28"); // This is the key to the word "Shop"
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shop)
    {
        if (firstButton)
        {
            shop = "Shop";
        }
    }

    public override void AddShops()
    {
        new NPCShop(Type, "Shop")
            .AddWithCustomValue(ItemID.RocketIII, Item.buyPrice(0, 0, 0, 50))
            .AddWithCustomValue(ItemID.RocketIV, Item.buyPrice(0, 0, 2, 50))
            .AddWithCustomValue(ItemID.Grenade, Item.buyPrice(0, 0, 0, 25))
            .Add<BobmOnAStick>()
            .Add<AnvilAndPropane>(Condition.DownedPlantera)
            .Add<Eagle500kgBomb>(Condition.DownedMoonLord)
            .Register();
    }

    public override void TownNPCAttackStrength(ref int damage, ref float knockback)
    {
        damage = 10;
        knockback = 2f;
    }

    public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
    {
        cooldown = 10;
        randExtraCooldown = 1;
    }

    public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
    {
        projType = ProjectileID.RocketI;
        attackDelay = 4;

        // Progressively delays subsequent shots
        if (NPC.localAI[3] > attackDelay)
        {
            attackDelay = 12;
        }
        if (NPC.localAI[3] > attackDelay)
        {
            attackDelay = 24;
        }
    }

    public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
    {
        multiplier = 10f;
        randomOffset = 0.2f;
    }

    public override void TownNPCAttackShoot(ref bool inBetweenShots)
    {
        if (NPC.localAI[3] > 1)
        {
            inBetweenShots = true;
        }
    }
}

public class OldManBubble : ModEmoteBubble
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CreepOldManBubble);
    public override void SetStaticDefaults()
    {
        AddToCategory(EmoteID.Category.Town);
    }

    public override Rectangle? GetFrame()
    {
        return new Rectangle(EmoteBubble.frame * 34, 0, 34, 28);
    }

    public override Rectangle? GetFrameInEmoteMenu(int frame, int frameCounter)
    {
        return new Rectangle(frame * 34, 0, 34, 28);
    }
}