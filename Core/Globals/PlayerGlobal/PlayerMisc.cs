using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class PlayerMisc : ModPlayer
{
    /// <summary>
    /// Acts as the <see cref="Main.GameUpdateCount"/> for a player without any arbitrary resets
    /// </summary>
    public uint GlobalTimer;

    public float HealingPotBonus = 1f;

    public override void UpdateDead()
    {
        HealingPotBonus = 1f;
    }

    public override void ResetEffects()
    {
        HealingPotBonus = 1f;
    }

    public override void PostUpdateMiscEffects()
    {
        if (Player.GetModPlayer<RejuvenationArtifactPlayer>().Equipped)
        {
            HealingPotBonus += 0.5f;
        }
    }

    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
    {
        healValue = (int) (healValue * HealingPotBonus);
    }

    public override void PostUpdate()
    {
        GlobalTimer++;
    }
}
