using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class Overheat : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Overheat);

    public override void SetStaticDefaults()
    {
        Main.pvpBuff[Type] = true;
        Main.debuff[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var modPlayer = player.GetModPlayer<LaserResource>();
        modPlayer.HeatRegenRate *= 6.5f;

        player.GetModPlayer<GlobalPlayer>().Overheat = true;

        player.statLife -= Main.rand.NextBool(5) ? 1 : 0;
        if (player.statLife <= 0)
        {
            player.statLife = 0;
            player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.TheExtraordinaryAdditions.Status.Death.Overheat", player.name)), 10, 1, false);
        }
    }
}