using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class CyberneticRocketGauntlets : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CyberneticRocketGauntlets);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Cyan);
    }

    public override void SetDefaults()
    {
        Item.damage = 1400;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.width = 54;
        Item.height = 60;
        Item.useTime = 4;
        Item.useAnimation = 4;
        Item.knockBack = 6f;
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<CyberneticRarity>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CyberneticSwing>();
        Item.crit = 0;
        Item.shootSpeed = 11f;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.None;
    }

    public override void HoldItem(Player player)
    {
        if (Main.mouseItem.IsAir && player.ownedProjectileCounts[Item.shoot] <= 0)
        {
            GlobalPlayer gp = player.Additions();
            CyberneticPlayer cyber = player.GetModPlayer<CyberneticPlayer>();

            if (gp.SafeMouseLeft.JustPressed)
                AddInput(player, 'L');
            if (gp.SafeMouseRight.JustPressed)
                AddInput(player, 'R');

            string sequence = new string(cyber.inputSequence.ToArray());
            if (sequence.StartsWith("L"))
                player.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2);
            if (sequence.StartsWith("R"))
                player.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, player.direction == -1 ? MathHelper.Pi : 0f);
        }
    }

    public void AddInput(Player player, char input)
    {
        GlobalPlayer gp = player.Additions();
        CyberneticPlayer cyber = player.GetModPlayer<CyberneticPlayer>();
        cyber.inputSequence.Add(input);
        cyber.comboTimer = CyberneticPlayer.COMBO_WINDOW;
        cyber.clickTimer = CyberneticPlayer.CLICK_WAIT;

        string sequence = new string(cyber.inputSequence.ToArray());
        CyberneticSwing.SwingState? state;

        if (sequence.EndsWith("LL"))
            state = CyberneticSwing.SwingState.LightningPunch;
        else if (sequence.EndsWith("LR"))
            state = CyberneticSwing.SwingState.Parry;
        else if (sequence.EndsWith("RR"))
            state = CyberneticSwing.SwingState.SMASH;
        else if (sequence.EndsWith("RL"))
            state = CyberneticSwing.SwingState.Uppercut;
        else
            state = null;

        if (state != null)
        {
            CyberneticSwing swing = Main.projectile[Projectile.NewProjectile(new EntitySource_ItemUse_WithAmmo(player, Item, Item.ammo),
                player.Center, player.Center.SafeDirectionTo(player.Additions().mouseWorld),
                Item.shoot, Item.damage, Item.knockBack, Main.myPlayer, 0f, 0f, 0f)].As<CyberneticSwing>();
            swing.State = state.Value;

            cyber.inputSequence.Clear();
        }
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanShoot(Player player) => false;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => false;

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.Cyan.ToVector3() * 1.8f);
    }
}

public class CyberneticPlayer : ModPlayer
{
    public List<char> inputSequence = new List<char>();
    public int comboTimer = 0;
    public const int COMBO_WINDOW = 30;
    public int clickTimer = 0;
    public const int CLICK_WAIT = 10;
    public int parryWait;

    public override void PostUpdateMiscEffects()
    {
        if (clickTimer > 0)
            clickTimer--;
        if (comboTimer > 0)
            comboTimer--;
        if (comboTimer <= 0)
            inputSequence.Clear();

        if (parryWait > 0)
        {
            parryWait--;
        }
    }
}