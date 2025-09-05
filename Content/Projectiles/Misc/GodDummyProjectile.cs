using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Misc;

public class GodDummyProjectile : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
    }
    public override bool? CanDamage() => false;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedPlayer => Owner.Additions();
    public override void OnKill(int timeLeft)
    {
        int i = NPC.NewNPC(NPC.GetBossSpawnSource(Main.myPlayer), (int)Projectile.Center.X, (int)Projectile.Center.Y, (int)Projectile.ai[0], 0, 0f, 0f, 0f, 0f, 255);
        NPC npc = Main.npc[i];
        if (i.WithinBounds(Main.maxNPCs))
        {
            npc.AdditionsInfo().ExtraAI[0] = Owner.whoAmI;
            npc.AdditionsInfo().ExtraAI[1] = ModdedPlayer.DummyMoveSpeed;
            npc.AdditionsInfo().ExtraAI[2] = Owner.whoAmI;
            npc.boss = ModdedPlayer.DummyBoss;
            npc.life = ModdedPlayer.DummyMaxLife;
            npc.lifeMax = ModdedPlayer.DummyMaxLife;
            npc.defense = ModdedPlayer.DummyDefense;
            npc.scale = ModdedPlayer.DummyScale;
            npc.rotation = ModdedPlayer.DummyRotation;
            npc.noGravity = !ModdedPlayer.DummyGravity;
            npc.direction = (Owner.Center.X < npc.Center.X).ToDirectionInt();
            npc.netUpdate = true;
            Projectile.netUpdate = true;
        }

        if (i != Main.maxNPCs && Main.dedServ)
        {
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i, 0f, 0f, 0f, 0, 0, 0);
        }
    }
}