using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;

public class AdditionsProjectileInfo : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => true;

    public const byte TotalExtraAISlots = 20;
    public float[] ExtraAI = new float[TotalExtraAISlots];

    public override void SetDefaults(Projectile projectile)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count ||
            projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = 0f;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count ||
            projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            binaryWriter.Write(ExtraAI[i]);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count ||
            projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = binaryReader.ReadSingle();
    }
}
