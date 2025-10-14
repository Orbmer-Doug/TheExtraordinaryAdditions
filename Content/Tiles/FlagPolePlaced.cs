using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.Tiles;

#nullable enable

public class FlagPolePlaced : ModTile
{
    public const int Width = 1;
    public const int Height = 11;

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlagPolePlaced);

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
        TileObjectData.newTile.Width = Width;
        TileObjectData.newTile.Height = Height;
        TileObjectData.newTile.Origin = new Point16(0, Height - 1);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, TileObjectData.newTile.Height).ToArray();
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        DustType = DustID.Lead;

        // Set the respective tile entity as a secondary element to incorporate when placing this tile
        ModTileEntity tileEntity = ModContent.GetInstance<FlagTileEntity>();
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, true);

        TileObjectData.addTile(Type);
        AddMapEntry(new Color(89, 86, 82), CreateMapEntryName());

        HitSound = SoundID.Tink;
    }

    public static T? FindTileEntity<T>(int i, int j, int width, int height) where T : ModTileEntity
    {
        // Find the top left corner of the FrameImportant tile that the player clicked on in the world.
        Tile t = Main.tile[i, j];
        int left = i - t.TileFrameX % (width * 18) / 18;
        int top = j - t.TileFrameY % (height * 18) / 18;

        int tileEntityID = ModContent.GetInstance<T>().Type;
        bool exists = TileEntity.ByPosition.TryGetValue(new Point16(left, top), out TileEntity? te);
        return exists && te!.type == tileEntityID ? (T)te : null;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        Tile tile = Main.tile[i, j];
        int left = i - tile.TileFrameX % (Width * 16) / 16;
        int top = j - tile.TileFrameY % (Height * 16) / 16;

        // Kill the hosted tile entity directly and immediately.
        FlagTileEntity? flag = FindTileEntity<FlagTileEntity>(i, j, Width, Height);
        flag?.Kill(left, top);
    }

    public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];
        FlagTileEntity? flag = FindTileEntity<FlagTileEntity>(i, j, Width, Height);
        if (flag is null || tile.TileFrameY != (Height - 2) * 18)
            return;

        int verticalOffset = 1 - Height;
        Color lightColorTopLeft = Lighting.GetColor(i, j + verticalOffset);
        Color lightColorTopRight = Lighting.GetColor(i + 3, j + verticalOffset);
        Color lightColorBottomLeft = Lighting.GetColor(i, j + verticalOffset + 2);
        Color lightColorBottomRight = Lighting.GetColor(i + 3, j + verticalOffset + 2);
        VertexColors colors = new VertexColors(lightColorTopLeft, lightColorTopRight, lightColorBottomRight, lightColorBottomLeft);

        Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
        drawOffset.Y += 35f;

        Vector2 start = new Vector2(i, j - Height + 1).ToWorldCoordinates() - Main.screenPosition + drawOffset;
        flag.FlagCloth?.Draw(AdditionsMain.Icon.Value, start, colors);
    }
}

public class FlagTileEntity : ModTileEntity, IClientSideTileEntityUpdater
{
    /// <summary>
    /// The flag cloth that this tile entity holds locally
    /// </summary>
    public ClothSimulationTiles FlagCloth
    {
        get;
        private set;
    } = new ClothSimulationTiles(30, 15, 3.3f);

    public override bool IsTileValidForEntity(int x, int y)
    {
        return true;
    }

    public void ClientSideUpdate_RenderCycle()
    {
        FlagCloth?.Update(new Vector2(Position.X, Position.Y + (FlagCloth.GridHeight / 16)).ToWorldCoordinates());
    }

    public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
    {
        // If in multiplayer, tell the server to place the tile entity and DO NOT place it yourself. That would mismatch IDs
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);
            return -1;
        }
        int placedID = Place(i, j);
        return placedID;
    }

    // Sync the tile entity the moment it is place on the server
    // This is done to cause it to register among all clients
    public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
}

public interface IClientSideTileEntityUpdater
{
    void ClientSideUpdate()
    {

    }
    void ClientSideUpdate_RenderCycle()
    {

    }
}

public class ClientServerTileEntityUpdateSystem : ModSystem
{
    public override void PreUpdateEntities()
    {
        foreach (TileEntity te in TileEntity.ByID.Values)
        {
            if (te is IClientSideTileEntityUpdater updater)
                updater.ClientSideUpdate();
        }
    }

    public override void PostDrawTiles()
    {
        foreach (TileEntity te in TileEntity.ByID.Values)
        {
            if (te is IClientSideTileEntityUpdater updater)
                updater.ClientSideUpdate_RenderCycle();
        }
    }
}