using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Back)]
public class CryogenicSpaceCanister : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.CryogenicSpaceCanister.Path;

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
        player.buffImmune[
            BuffID.OnFire & BuffID.OnFire3 & BuffID.Burning & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Frozen &
            BuffID.Slow & BuffID.Chilled] = true;
        player.resistCold = true;
        player.GetModPlayer<NitrogenCoolingPackPlayer>().Equipped = true;

        ref bool cryo = ref player.GetModPlayer<CryogenicSpaceCanisterPlayer>().Equipped;
        {
            cryo = true;
            player.statDefense += 20;
        }

        {
            cryo = false;
            player.GetArmorPenetration(DamageClass.Generic) += 15;
        }
    }

    public override void AddRecipes()
    {
        // TODO
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LunarBar, 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}

public sealed class CryogenicSpaceCanisterPlayer : ModPlayer
{
    public const int TimeForCryogenic = 600;

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
            AssetRegistry.GennedSounds.ColdHitMassive.Play(Player.Center, .7f, 0f, .1f);
            if (Main.myPlayer == Player.whoAmI)
                Player.CreatePlayerProj(Player.Center, Vector2.Zero, ModContent.ProjectileType<CryogenicBlast>(),
                    (int) Player.GetTotalDamage<GenericDamageClass>().ApplyTo(4000), 4f, Player.whoAmI);
            Counter = 0;
        }
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a,
        ref bool fullBright)
    {
        if (!Equipped)
            return;
        if (Counter is > 0 and < TimeForCryogenic)
        {
            if (Main.rand.NextBool(5) && drawInfo.shadow == 0)
            {
                for (int t = 0; t < 2; t++)
                {
                    Vector2 randPos = Main.rand.NextVector2CircularEdge(150f, 150f);
                    Vector2 pos = Player.Center + randPos;
                    Vector2 vel = Player.DirectionFrom(Player.Center + Player.velocity + randPos) *
                                  Main.rand.NextFloat(7f, 9f);
                    ParticleRegistry.SpawnSparkParticle(pos, vel, 30, InverseLerp(0f, TimeForCryogenic, Counter),
                        Color.DarkSlateBlue, false, false, Player.Center);
                }
            }
        }
    }
}
