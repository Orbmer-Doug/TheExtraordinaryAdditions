using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Back)]
public class CryogenicSpaceCanister : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CryogenicSpaceCanister);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.LightCyan);
    }

    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 62;
        Item.maxStack = 1;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.accessory = true;
        Item.rare = ItemRarityID.Red;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        LaserResource resource = player.GetModPlayer<LaserResource>();

        player.buffImmune[BuffID.OnFire & BuffID.OnFire3 & BuffID.Burning & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Frozen & BuffID.Slow & BuffID.Chilled] = true;
        player.resistCold = true;
        player.GetModPlayer<NitrogenCoolingPackPlayer>().Equipped = true;

        ref bool cryo = ref player.GetModPlayer<CryogenicSpaceCanisterPlayer>().Equipped;
        if (resource.HeatCurrent == 0)
        {
            cryo = true;
            player.statDefense += 20;
        }
        if (resource.HeatCurrent > 0)
        {
            cryo = false;
            resource.HeatRegenRate *= 2.7f;
            player.GetArmorPenetration(DamageClass.Generic) += 15;
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LunarBar, 10);
        recipe.AddIngredient(ModContent.ItemType<CryonicBar>(), 12);
        recipe.AddIngredient(ModContent.ItemType<CoreofEleum>(), 15);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}

public sealed class CryogenicSpaceCanisterPlayer : ModPlayer
{
    public static readonly int TimeForCryogenic = SecondsToFrames(10);

    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
    public int Counter;
    public override void UpdateDead() => Counter = 0;

    public override void PostUpdate()
    {
        if (!Equipped)
        {
            Counter = 0;
            return;
        }

        Counter++;
        if (Counter > TimeForCryogenic)
        {
            AdditionsSound.ColdHitMassive.Play(Player.Center, .7f, 0f, .1f);
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(Player.Center, Vector2.Zero, ModContent.ProjectileType<CryogenicBlast>(), (int)Player.GetTotalDamage<GenericDamageClass>().ApplyTo(4000), 4f, Player.whoAmI);
            Counter = 0;
        }
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        if (!Equipped)
            return;
        if (Counter.BetweenNum(0, TimeForCryogenic))
        {
            if (Main.rand.NextBool(5) && drawInfo.shadow == 0)
            {
                for (int t = 0; t < 2; t++)
                {
                    Vector2 randPos = Main.rand.NextVector2CircularEdge(150f, 150f);
                    Vector2 pos = Player.Center + randPos;
                    Vector2 vel = Player.DirectionFrom(Player.Center + Player.velocity + randPos) * Main.rand.NextFloat(7f, 9f);
                    ParticleRegistry.SpawnSparkParticle(pos, vel, 30, InverseLerp(0f, TimeForCryogenic, Counter),
                        Color.DarkSlateBlue, false, false, Player.Center);
                }
            }
        }
    }
}