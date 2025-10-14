using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

public class TungstenTie : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TungstenTie);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(247, 226, 218));
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 40;
        Item.accessory = true;
        Item.defense = 10;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.maxStack = 1;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<TungstenTiePlayer>().Equipped = true;

        player.GetArmorPenetration(DamageClass.Generic) += 10f;
        player.maxFallSpeed = 15f;
        player.fallStart = 1000;
        player.fallStart2 = 1000;
        player.thorns = 1f;
        player.moveSpeed -= 0.1f;
        player.runAcceleration -= .05f;
        player.ignoreWater = player.noKnockback = true;
        player.canFloatInWater = player.adjWater = player.waterWalk = player.waterWalk2 = player.jumpBoost = false;
        player.wingTimeMax -= 10;

        if (!Main.rand.NextBool(15))
            return;

        int tie = ModContent.ProjectileType<SharpTie>();
        int owned = player.CountOwnerProjectiles(tie);
        if (owned >= 10)
            return;

        for (int j = 0; j < 50; j++)
        {
            int area = Main.rand.Next(200 - j * 2, 400 + j * 2);
            Vector2 center = player.Center;
            center.X += Main.rand.Next(-area, area + 1) + 12;
            center.Y += Main.rand.Next(-area, area + 1) + 12;
            if (!Collision.CanHit(new Vector2(player.Center.X, player.position.Y), 1, 1, center, 1, 1)
                && !Collision.CanHit(new Vector2(player.Center.X, player.position.Y - 50f), 1, 1, center, 1, 1))
                continue;

            if (Main.myPlayer == player.whoAmI)
            {
                int damage = (int)player.GetDamage(DamageClass.Generic).ApplyTo(350f);
                Projectile.NewProjectileDirect(player.GetSource_Accessory(Item, null), center, Vector2.Zero,
                    tie, damage, 3f, player.whoAmI);
                for (int i = 0; i < 65; i++)
                {
                    float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 6f;
                    Dust dust = Dust.NewDustPerfect(center, DustID.AncientLight, shootVelocity, default, default, 1.6f);
                    dust.noGravity = true;
                }
                break;
            }
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 10);
        recipe.AddIngredient(ModContent.ItemType<AshersWhiteTie>(), 1);
        recipe.AddIngredient(ModContent.ItemType<TungstenCube>(), 1);
        recipe.AddTile(TileID.ClayBlock);
        recipe.AddTile(TileID.Loom);
        recipe.AddTile(TileID.LunarMonolith);
        recipe.Register();
    }
}

public sealed class TungstenTiePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (Equipped && !Player.HasBuff<TheTiesCooldown>() && !Player.GetModPlayer<AshersWhiteTiePlayer>().Equipped)
        {
            AdditionsSound.etherealNuhUh.Play(Player.Center, 1.4f, -.2f);
            for (int l = 0; l < 12; l++)
            {
                ParticleRegistry.SpawnPulseRingParticle(Player.Center, Vector2.Zero, 20, RandomRotation(), new(.5f, 1f),
                    0f, .15f, Color.DarkGray);
                ParticleRegistry.SpawnSparkParticle(Player.Center, Main.rand.NextVector2CircularLimited(12, 12f, .4f, 1f),
                    40, Main.rand.NextFloat(.4f, .6f), Color.Gray);
            }
            ParticleRegistry.SpawnThunderParticle(Player.Center, 140, 1.5f, new(1f), 0f, Color.WhiteSmoke);

            Player.Heal(100);
            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;

            Player.AddBuff(ModContent.BuffType<TheTiesCooldown>(), SecondsToFrames(210));
            return false;
        }

        return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
    }
}