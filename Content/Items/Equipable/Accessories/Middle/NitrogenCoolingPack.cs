using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

[AutoloadEquip(EquipType.Back)]
public class NitrogenCoolingPack : ModItem, ILocalizedModType
{
    public override string Texture => AssetRegistry.GennedTextures.NitrogenCoolingPack.Path;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(138, 204, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 62;
        Item.maxStack = 1;
        Item.defense = 2;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.accessory = true;
        Item.defense = 4;
        Item.rare = ItemRarityID.Yellow;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<NitrogenCoolingPackPlayer>().Equipped = true;
        player.buffImmune[
            BuffID.OnFire & BuffID.OnFire3 & BuffID.Burning & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Frozen &
            BuffID.Slow & BuffID.Chilled] = true;
        player.resistCold = true;
        player.GetArmorPenetration(DamageClass.Generic) += 10;
    }

    public override void AddRecipes()
    {
        // TODO
        Recipe recipe = CreateRecipe();
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}

public sealed class NitrogenCoolingPackPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
    public int Counter;
    public override void UpdateDead() => Counter = 0;

    public override void PostUpdateMiscEffects()
    {
        if (!Equipped)
            return;

        Item item = Player.HeldItem;

        if (Player.AdditionsMouse().SafeMouseLeft.Current && GlobalPlayer.HasDamageClass(Player) && item.damage > 0)
            Counter++;

        if (Counter >= 20)
        {
            Vector2 pos = Player.RotatedRelativePoint(Player.MountedCenter)
                          + PolarVector(10f * Player.direction, Player.fullRotation + MathHelper.Pi)
                          + PolarVector(4f * Player.direction * Player.gravDir,
                              Player.fullRotation + MathHelper.PiOver2);
            Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
            if (Main.myPlayer == Player.whoAmI)
                Player.CreatePlayerProj(pos, vel, ModContent.ProjectileType<IcyShards>(), DamageSoftCap(item.damage, 150),
                    1f, Main.myPlayer);
            Counter = 0;
        }
    }
}
