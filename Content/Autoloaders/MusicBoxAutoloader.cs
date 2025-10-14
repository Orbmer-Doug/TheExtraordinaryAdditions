using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TheExtraordinaryAdditions.Content.Autoloaders;

public static class MusicBoxAutoloader
{
    [Autoload(false)]
    public class AutoloadableMusicBoxItem : ModItem
    {
        private readonly string musicPath;

        private readonly string texturePath;

        private readonly string name;

        internal int tileID;

        public override string Name => name;

        public override string Texture => texturePath;

        public override bool CloneNewInstances => true;

        public AutoloadableMusicBoxItem(string texturePath, string musicPath)
        {
            string name = Path.GetFileName(texturePath);
            this.musicPath = musicPath;
            this.texturePath = texturePath;
            this.name = name;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            // Music boxes cant get prefixes in vanilla
            ItemID.Sets.CanGetPrefixes[Type] = false;

            // Recorded music boxes transform into the basic form in shimmer
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;

            // Register the music box with the desired music
            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, musicPath), Type, tileID);
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(tileID, 0); // this 0 is very important in making the item work apparently
        }
    }

    [Autoload(false)]
    public class AutoloadableMusicBoxTile(string texturePath) : ModTile
    {
        internal int itemID;

        private readonly string texturePath = texturePath;

        private readonly string name = Path.GetFileName(texturePath);

        public override string Name => name;
        public override string Texture => texturePath;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;

            AddMapEntry(new Color(150, 137, 142));
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = itemID;
        }

        public override bool CreateDust(int i, int j, ref int type) => false;

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            if (frameX >= 36)
                Item.NewItem(new EntitySource_TileBreak(i, j), new Point(i, j).ToWorldCoordinates(), itemID);
        }
    }

    public static void Create(Mod mod, string texPath, string tileTexPath, string musicPath, out int itemID)
    {
        AutoloadableMusicBoxItem boxItem = new AutoloadableMusicBoxItem(texPath, musicPath);
        mod.AddContent(boxItem);
        itemID = boxItem.Type;

        AutoloadableMusicBoxTile boxTile = new AutoloadableMusicBoxTile(tileTexPath);
        mod.AddContent(boxTile);
        int tileID = boxTile.Type;

        boxItem.tileID = tileID;
        boxTile.itemID = itemID;
    }
}