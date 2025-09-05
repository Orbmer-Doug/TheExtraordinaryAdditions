using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Social;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Neck)]
public class AshersWhiteTie : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AshersWhiteTie);
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 14;
        Item.accessory = true;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
    }
    public override void Update(ref float gravity, ref float maxFallSpeed)
    {
        Lighting.AddLight(Item.position, Color.White.ToVector3() * 2f);
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<GlobalPlayer>().AshersTie = true;
        ref StatModifier damage2 = ref player.GetDamage<GenericDamageClass>();
        damage2 += 0.1f;
        player.GetCritChance<GenericDamageClass>() += 10f;

        int damage = (int)player.GetDamage(DamageClass.Melee).ApplyTo(250f);
        float knockBack = 3f;
        if (!Main.rand.NextBool(15))
        {
            return;
        }
        int num = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI && Main.projectile[i].type == ModContent.ProjectileType<SharpTie>())
            {
                num++;
            }
        }
        IEntitySource source = player.GetSource_Accessory(Item, null);
        if (Main.rand.Next(15) < num || num >= 10)
        {
            return;
        }
        for (int j = 0; j < 50; j++)
        {
            int num5 = Main.rand.Next(200 - j * 2, 400 + j * 2);
            Vector2 center = player.Center;
            center.X += Main.rand.Next(-num5, num5 + 1);
            center.Y += Main.rand.Next(-num5, num5 + 1);
            center.X += 12;
            center.Y += 12;
            if (!Collision.CanHit(new Vector2(player.Center.X, player.position.Y), 1, 1, center, 1, 1) && !Collision.CanHit(new Vector2(player.Center.X, player.position.Y - 50f), 1, 1, center, 1, 1))
            {
                continue;
            }
            int num6 = (int)center.X / 16;
            int num7 = (int)center.Y / 16;
            bool flag = false;
            if (Main.rand.NextBool(3) && Main.tile[num6, num7] != null)
            {
                Tile val = Main.tile[num6, num7];
                if (val.WallType > 0)
                {
                    flag = true;
                    goto IL_028d;
                }
            }
            center.X -= 45;
            center.Y -= 45;
            {
                center.X += 45;
                center.Y += 45;
                flag = true;
            }
            goto IL_028d;
        IL_028d:
            if (!flag)
            {
                continue;
            }
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                if (Main.projectile[k].active && Main.projectile[k].owner == player.whoAmI && Main.projectile[k].type == ModContent.ProjectileType<SharpTie>())
                {
                    Vector2 val2 = center - Main.projectile[k].Center;
                    if (((Vector2)val2).Length() < 48f)
                    {
                        flag = false;
                        break;
                    }
                }
            }
            if (flag && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(source, center.X, center.Y, 0f, 0f, ModContent.ProjectileType<SharpTie>(), damage, knockBack, player.whoAmI, 0f, 0f, 0f);
                for (int i = 0; i < 40; i++)
                {
                    float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 4f;
                    Dust dust = Dust.NewDustPerfect(center, DustID.AncientLight, shootVelocity, default, default, 1.6f);
                    dust.noGravity = true;
                }


                break;
            }
        }
    }
}
