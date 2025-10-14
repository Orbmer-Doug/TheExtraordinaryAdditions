using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class AstralDashCooldown : CooldownHandler
{
    public static new string ID => "AstralDash";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownAstralDash);
    public override Color OutlineColor => Color.White;
    public override Color CooldownStartColor => new(66, 189, 181);
    public override Color CooldownEndColor => new(109, 242, 196);
    public override void Tick()
    {
        if (Main.rand.NextBool(4))
        {
            Vector2 center = instance.player.RotatedRelativePoint(instance.player.MountedCenter, true, true);
            Vector2 pos = center + Main.rand.NextVector2CircularLimited(200f, 200f, .7f, 1f);
            Vector2 vel = RandomVelocity(1f, 1f, 4f);
            int life = Main.rand.Next(40, 100);
            float scale = Main.rand.NextFloat(.3f, .5f);
            Color col = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly * 1.4f), AstralKatanaSweep.AstralBluePalette);
            Color col2 = MulticolorLerp(Cos01(Main.GlobalTimeWrappedHourly * 1.4f), AstralKatanaSweep.AstralOrangePalette);
            Color color = Main.rand.NextBool() ? col : col2;
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, color, color, center, 1.1f);
        }
    }
}
