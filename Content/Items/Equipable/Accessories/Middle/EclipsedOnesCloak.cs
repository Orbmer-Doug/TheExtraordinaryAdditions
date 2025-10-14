using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

[AutoloadEquip(EquipType.Front)]
public class EclipsedOnesCloak : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EclipsedOnesCloak);
    public override void SetDefaults()
    {
        Item.width = 34;
        Item.height = 30;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.defense = 4;
        Item.accessory = true;
    }

    public override void UpdateEquip(Player player)
    {
        player.buffImmune[BuffID.Chilled & BuffID.Frostburn & BuffID.Frostburn2] = true;

        int type = ModContent.ProjectileType<EclipsedAura>();
        if (player.CountOwnerProjectiles(type) <= 0 && Main.myPlayer == player.whoAmI)
        {
            int p = player.NewPlayerProj(player.Center, Vector2.Zero, type, 1, 0f, player.whoAmI);
            Main.projectile[p].DamageType = player.GetBestClass();
        }

        player.GetModPlayer<EclipsedOnesCloakPlayer>().Equipped = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Silk, 20);
        recipe.AddIngredient(ItemID.FlinxFur, 6);
        recipe.AddIngredient(ItemID.IceBlock, 75);
        recipe.AddIngredient(ItemID.Bone, 16);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}

public sealed class EclipsedOnesCloakPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
    public override void PostUpdate()
    {
        if (!Equipped)
            return;

        bool active = Player.active && !Player.DeadOrGhost;
        if (active && Player.velocity.Length() != 0 && !Player.mount.Active)
        {
            Vector2 randPos = Player.RotatedRelativePoint(Player.MountedCenter) + PolarVector(Player.height / 2 * Player.gravDir, Player.fullRotation + MathHelper.PiOver2) + PolarVector(Main.rand.NextFloat(-Player.width / 2, Player.width / 2), Player.fullRotation);
            Vector2 vel = -Player.velocity.RotatedByRandom(.12f) * Main.rand.NextFloat(.1f, .4f);
            ParticleRegistry.SpawnDustParticle(randPos, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), Color.LightCyan);
        }
    }
}