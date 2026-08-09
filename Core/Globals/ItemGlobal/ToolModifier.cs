using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.CrossCompatibility;

namespace TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

public sealed class ToolModifier : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        if (!AdditionsConfigServer.Instance.ToolOverhaul)
            return false;

        if (!lateInstantiation)
            return false;

        if (!(ItemID.Sets.IsChainsaw[entity.type] || ItemID.Sets.IsDrill[entity.type]))
        {
            if (entity.channel)
                return false;
        }

        bool allowedType = true;
        if (ModReferences.Fables != null)
        {
            if (ModReferences.Fables.TryFind("MarniteObliterator", out ModItem fabmar) && entity.type == fabmar.Type)
                allowedType = false;
            if (ModReferences.Fables.TryFind("MarniteDeconstructor", out ModItem fabobl) && entity.type == fabobl.Type)
                allowedType = false;
        }

        if (entity.type is ItemID.ButchersChainsaw or ItemID.LaserDrill or ItemID.ChlorophyteJackhammer)
            allowedType = false;

        return (entity.pick > 0 || entity.axe > 0 || entity.hammer > 0) && allowedType;
    }

    public override void SetDefaults(Item item)
    {
        ItemID.Sets.SkipsInitialUseSound[item.type] = item.noMelee = item.noUseGraphic = true;
        if (item.channel)
        {
            if (ItemID.Sets.IsDrill[item.type])
                item.shoot = ModContent.ProjectileType<FancyDrill>();
            if (ItemID.Sets.IsChainsaw[item.type])
                item.shoot = ModContent.ProjectileType<FancyChainsaw>();
        }
        else
            item.shoot = ModContent.ProjectileType<FancyTool>();
    }

    public override void HoldItem(Item item, Player player)
    {
        // Might be unsafe to do, but it prevents the inital check from using the item
        player.toolTime = 20;
        player.controlUseItem = false;
    }

    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback) => false;

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
    {
        if (Main.myPlayer == player.whoAmI && player.itemAnimation == player.itemAnimationMax &&
            player.ownedProjectileCounts[item.shoot] <= 0)
        {
            Projectile.NewProjectile(new EntitySource_ItemUse_WithAmmo(player, item, item.ammo),
                player.Center, Vector2.Zero, item.shoot, item.damage, item.knockBack, player.whoAmI);
        }
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        const string key = "ToolModifier.";
        if (item.hammer > 0)
        {
            const string name = "HammerInfo";
            string text = GetText(key + name).Value;
            tooltips.Add(new TooltipLine(Mod, name, text));
        }

        if (ItemID.Sets.IsChainsaw[item.type])
        {
            const string name = "ChainsawInfo";
            string text = GetText(key + name).Value;
            tooltips.Add(new TooltipLine(Mod, name, text));
        }
    }
}
