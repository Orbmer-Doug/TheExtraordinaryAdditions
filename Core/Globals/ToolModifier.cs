using CalamityMod.Items.Tools;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.CrossCompatibility;

namespace TheExtraordinaryAdditions.Core.Globals;

public class ToolModifier : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        if (AdditionsConfigServer.Instance.ToolOverhaul == false)
            return false;

        if (lateInstantiation)
        {
            if (!(ItemID.Sets.IsChainsaw[entity.type] || ItemID.Sets.IsDrill[entity.type]))
            {
                if (entity.channel == true)
                    return false;
            }

            bool allowedType = true;
            if (ModReferences.Fables != null)
            {
                if (ModReferences.Fables.TryFind<ModItem>("MarniteObliterator", out ModItem fabmar) && entity.type == fabmar.Type)
                    allowedType = false;
                if (ModReferences.Fables.TryFind<ModItem>("MarniteDeconstructor", out ModItem fabobl) && entity.type == fabobl.Type)
                    allowedType = false;
            }
            if (entity.type == ItemID.ButchersChainsaw || entity.type == ItemID.LaserDrill ||
                entity.type == ItemID.ChlorophyteJackhammer || entity.type == ModContent.ItemType<MarniteObliterator>() || entity.type == ModContent.ItemType<MarniteDeconstructor>())
                allowedType = false;

            return entity.pick > 0 || entity.axe > 0 || entity.hammer > 0 && allowedType;
        }
        return false;
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

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
    {
        if (Main.myPlayer == player.whoAmI && player.itemAnimation == player.itemAnimationMax && player.ownedProjectileCounts[item.shoot] == 0)
        {
            Projectile.NewProjectile(new EntitySource_ItemUse_WithAmmo(player, item, item.ammo),
                player.Center, Vector2.Zero, item.shoot, item.damage, item.knockBack, player.whoAmI, 0f, 0f, 0f);
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