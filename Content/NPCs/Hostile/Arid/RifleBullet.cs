using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class RifleBullet : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RifleBullet);
}