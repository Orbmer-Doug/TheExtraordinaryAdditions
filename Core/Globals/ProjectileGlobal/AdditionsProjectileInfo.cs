using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;

/* look, i know some of this is incorrect but im not going to deal with the plausibility that some random projectile not from the mod is going to try to access
   this global somehow and it crashes the game (plus it doesn't sound very pleasant to have to replace every single instance of extra ai in the entire mod)
    
   from actual logs this is what i get after using the Etheral Claymore (or really anything) from a specific frame:   
   System.Collections.Generic.KeyNotFoundException: Acid Bubble (ID: 1260) is not registered in AdditionsProjectileInfo <-- Acid Bubble is from Calamity
   at TheExtraordinaryAdditions.Core.Globals.GlobalExtensions.AdditionsInfo(Projectile projectile)
   at TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle.EtherealSwing.get_FadeTimer()
   
   its also not easily reputable.

   i dont know if i just dont see a fix but any advice would be nice thanks
*/

public class AdditionsProjectileInfo : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => true;

    public const byte TotalExtraAISlots = 20;
    public float[] ExtraAI = new float[TotalExtraAISlots];

    public override void SetDefaults(Projectile projectile)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count || projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = 0f;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count || projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            binaryWriter.Write(ExtraAI[i]);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count || projectile.ModProjectile.Mod != AdditionsMain.Instance)
            return;
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = binaryReader.ReadSingle();
    }
}
