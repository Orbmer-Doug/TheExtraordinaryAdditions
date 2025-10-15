using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Autoloaders;

public static class MaskAutoloader
{
    [Autoload(false)]
    [AutoloadEquip(EquipType.Head)]
    public class AutoloadableMask : ModItem
    {
        private readonly string texturePath;

        private readonly bool drawHead;

        private readonly string name;

        public override string Name => name;

        public override string Texture => texturePath;

        public override bool CloneNewInstances => true;

        public AutoloadableMask(string texturePath, bool permitDefaultHeadDrawing)
        {
            string name = Path.GetFileName(texturePath);
            this.drawHead = permitDefaultHeadDrawing;
            this.texturePath = texturePath;
            this.name = name;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            if (Main.netMode != NetmodeID.Server)
            {
                int headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
                ArmorIDs.Head.Sets.DrawHead[headSlot] = drawHead;
                ArmorIDs.Head.Sets.DrawFullHair[headSlot] = drawHead;
            }
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 24;
            Item.rare = ItemRarityID.Blue;
            Item.vanity = true;
            Item.maxStack = 1;
        }
    }

    public static int Create(Mod mod, string maskPath, bool drawHead)
    {
        AutoloadableMask mask = new AutoloadableMask(maskPath, drawHead);
        mod.AddContent(mask);
        return mask.Type;
    }
}