using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Head)]
public class AbsoluteGreathelm : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GennedTextures.AbsoluteGreathelm.Path;
    public static int HeadSlotID { get; private set; }

    public override void SetStaticDefaults()
    {
        HeadSlotID = Item.headSlot;
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 26;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.vanity = true;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<AbsoluteCoreplate>())
        {
            return legs.type == ModContent.ItemType<AbsoluteGreaves>();
        }

        return false;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, Color.AntiqueWhite.ToVector3() * 1.5f);

        ref StatModifier damage = ref player.GetDamage<GenericDamageClass>();
        damage += 0.35f;
        player.GetCritChance<GenericDamageClass>() += 25f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<VoltHelmet>());
        recipe.AddIngredient(ModContent.ItemType<SpecteriteMask>());
        recipe.AddIngredient(ModContent.ItemType<BlueTopHat>());
        recipe.AddIngredient(ModContent.ItemType<TremorGreathelm>());
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}

public sealed class AbsoluteArmorPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    public override void UpdateLifeRegen()
    {
        if (!Equipped)
            return;

        Player.lifeRegenCount += 4;

        while (Player.lifeRegenCount >= 120)
        {
            Player.lifeRegenCount -= 120;

            if (Player.statLife < Player.statLifeMax2)
            {
                Player.statLife += 2;
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 pos = Player.Center;
                        float lightSpawnOffset = Main.rand.NextFloat(120f, 120f);
                        Vector2 lightSpawnPosition = pos + Main.rand.NextVector2Unit() * lightSpawnOffset;
                        Vector2 lightSpawnVelocity = (pos - lightSpawnPosition) * 0.0411f;
                        float particleScale = Main.rand.NextFloat(.4f, 1.1f);
                        ParticleRegistry.SpawnSparkParticle(lightSpawnPosition, lightSpawnVelocity, 40, particleScale,
                            Color.AntiqueWhite, false, false, pos);
                    }
                }
            }

            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;
        }
    }
}
