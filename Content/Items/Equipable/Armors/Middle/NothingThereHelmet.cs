using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Head)]
public class NothingThereHelmet : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.NothingThereHelmet);
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = true;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.defense = 15;
    }

    public override void UpdateArmorSet(Player player)
    {
        player.setBonus = this.GetLocalization("SetBonus").Format(AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString());
        player.moveSpeed += 0.3f;
        ref int counter = ref player.Additions().NothingThereCounter;
        ref int wait = ref player.Additions().NothingThereWait;
        int time = SecondsToFrames(4);

        // Start knashing
        if (AdditionsKeybinds.SetBonusHotKey.JustPressed && counter <= 0 && wait <= 0 && player.whoAmI == Main.myPlayer)
        {
            Vector2 pos = player.Center + new Vector2(0f, -20f);
            for (float i = .1f; i < .3f; i += .1f)
            {
                int life = 20 + (int)(i * 15);
                ParticleRegistry.SpawnPulseRingParticle(pos, player.velocity, life, RandomRotation(), new(1f), 0f, .1f + i, Color.Crimson, true);

                for (int a = 0; a < 25; a++)
                {
                    ParticleRegistry.SpawnBloodParticle(pos, Main.rand.NextVector2CircularLimited(9f, 9f, .5f, 1f),
                        Main.rand.Next(60, 90), Main.rand.NextFloat(.9f, 1.4f), Color.DarkRed * 1.4f);
                }
            }

            SoundEngine.PlaySound(SoundID.ForceRoarPitched with { Pitch = -.3f, Volume = 1.2f }, pos);
            counter = time;
        }

        // Start cooldown right before finishing
        if (counter == 1)
        {
            wait = time * 3;
        }

        // Summon the teeth while counting down
        if (counter % 5f == 4f)
        {
            Vector2 mouse = player.Additions().mouseWorld;
            float rand = Main.rand.NextFloat(300f, 300f);
            Vector2 pos = mouse + Main.rand.NextVector2CircularEdge(rand, rand);
            Vector2 vel = pos.SafeDirectionTo(mouse) * Main.rand.NextFloat(10f, 20f);
            int type = ModContent.ProjectileType<KnashingTeeth>();
            int dmg = 225;
            float kb = 0f;
            float turnX = Main.rand.NextFloat(-.02f, .02f);
            float turnY = Main.rand.NextFloat(-.02f, .02f);
            player.NewPlayerProj(pos, vel, type, dmg, kb, player.whoAmI, 0f, turnX, turnY);
            SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { MaxInstances = 0, PitchVariance = .3f, Volume = 1.2f }, pos, null);
        }

        // Cooldown effects
        if (wait > 0 && Main.rand.NextBool(6))
        {
            ParticleRegistry.SpawnPulseRingParticle(player.RandAreaInEntity(), player.velocity, 20, RandomRotation(), new(Main.rand.NextFloat(.4f, .8f), 1f), 0f, 60f, Color.Crimson, true);
        }

        player.Additions().NothingThere = true;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == Mod.Find<ModItem>("MimicryChestplate").Type)
        {
            return legs.type == Mod.Find<ModItem>("MimicryLeggings").Type;
        }
        return false;
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Magic) += 0.17f;
        player.GetDamage(DamageClass.Summon) += 0.17f;
        player.manaCost *= 0.85f;
        player.statManaMax2 += 50;
        player.maxMinions += 2;
        player.maxTurrets += 1;
        player.buffImmune[BuffID.Chilled] = true;
    }
}