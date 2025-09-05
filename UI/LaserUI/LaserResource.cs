using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

namespace TheExtraordinaryAdditions.UI.LaserUI;

public class LaserResource : ModPlayer
{
    public static int OverheatBuff = ModContent.BuffType<Overheat>();
    public const int OverheatBuffTime = 870;
    public static SoundStyle OverheatSound = AssetRegistry.GetSound(AdditionsSound.AdrenalineMajorLoss) with { Volume = 1.5f };

    public int HeatCurrent;
    public const int HeatMaximum = 100;
    public int HeatMax; // Buffer variable that is used to reset maximum resource to default value
    public int HeatMax2; // Maximum amount
    public float HeatRegenRate;
    internal int HeatRegenTimer = 0;
    public float HeatBarAlpha = 1f;

    public bool HoldingLaserWeapon;

    public override void Initialize()
    {
        HeatCurrent = 0;
        HeatMax = HeatMaximum;
    }

    public override void ResetEffects()
    {
        ResetVariables();
    }

    public override void UpdateDead()
    {
        HeatBarAlpha = MathHelper.Lerp(HeatBarAlpha, 0f, 0.035f);
        ResetVariables();
    }

    private void ResetVariables()
    {
        HeatRegenRate = 1f;
        HeatMax2 = HeatMax;
        HoldingLaserWeapon = false;
    }

    public override void PostUpdateMiscEffects()
    {
        HeatBarAlpha = MathHelper.Lerp(HeatBarAlpha, 1f, 0.035f);

        // Dont continue burning
        if (Player.HasBuff(OverheatBuff) && HeatCurrent == 0)
            Player.ClearBuff(OverheatBuff);

        // honestly this is stupid but theres no incentive to make a system for a few items
        ModItem p = Player.HeldItem.ModItem;
        if (p != null && (p is HeavyLaserRifle))
        {
            HoldingLaserWeapon = true;
        }
        if (Player.HeldItem != null && Player.HeldItem.type == ItemID.HeatRay)
            HoldingLaserWeapon = true;

        HeatRegenTimer++;
        if (HeatRegenTimer > 50 / HeatRegenRate)
        {
            HeatCurrent -= 1;
            HeatRegenTimer = 0;
        }
        HeatCurrent = Utils.Clamp(HeatCurrent, 0, HeatMax2);
    }
    public override void PostUpdate()
    {
        if (Main.myPlayer == Player.whoAmI && Player.creativeGodMode)
            HeatCurrent = 0;
    }

    public static bool CanFire(Player player) => !player.HasBuff(OverheatBuff);
    public static void ApplyLaserOverheating(Player player)
    {
        LaserResource laser = player.GetModPlayer<LaserResource>();
        if (laser.HeatCurrent >= laser.HeatMax2)
        {
            SoundEngine.PlaySound(OverheatSound);
            player.AddBuff(OverheatBuff, OverheatBuffTime);
        }
    }
}