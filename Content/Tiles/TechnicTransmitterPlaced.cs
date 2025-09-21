using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Content.World.Subworlds;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class TechnicTransmitterPlaced : ModTile
{
    public const int Width = 8;
    public const int Height = 12;

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TechnicTransmitterPlaced);

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
        TileObjectData.newTile.Width = Width;
        TileObjectData.newTile.Height = Height;
        TileObjectData.newTile.Origin = new Point16(Width / 2, Height - 1);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, TileObjectData.newTile.Height).ToArray();
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        DustType = DustID.t_Martian;

        TileObjectData.addTile(Type);
        AddMapEntry(new Color(0, 230, 242), CreateMapEntryName());

        HitSound = SoundID.Tink;
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;

        int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
        player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
    }

    public override bool RightClick(int i, int j)
    {
        if (Main.ActiveWorldFileData.IsCloudSave)
        {
            Utility.DisplayText(this.GetLocalizedValue("CloudSave"));
            return false;
        }

        int type = ModContent.ProjectileType<TransmitterLightspeed>();
        if (!Utility.AnyProjectile(type))
        {
            Tile tile = Main.tile[i, j];
            int snapX = tile.TileFrameX / 18;
            int snapY = tile.TileFrameY / 18;
            int topLeftX = i - snapX;
            int topLeftY = j - snapY;

            Point pos = new Point(topLeftX + Width / 2 - 1, topLeftY);
            Vector2 position = pos.ToWorldCoordinates();
            position.X += 10;
            position.Y += 4;
            IEntitySource source = Main.LocalPlayer.GetProjectileSource_TileInteraction(pos.X, pos.Y);
            int index = Projectile.NewProjectile(source, position, Vector2.Zero, type, 0, 0f, Main.myPlayer, 0f, 0f, 0f);
            Main.projectile[index].originatedFromActivableTile = true;
        }
        return true;
    }
}

public class TransmitterLightspeed : ModProjectile, IHasScreenShader
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(1f);
        Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.hostile = Projectile.tileCollide = false;
    }

    public const float CollapseDuration = 2.0f;
    public const float CollapseMinPower = 1.0f;
    public const float CollapseMaxPower = 10.0f;
    public const float ScaleDuration = 2.0f;
    public const float TotalDuration = CollapseDuration + ScaleDuration;

    public ref float Time => ref Projectile.ai[0];
    public bool Reverse
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }
    public override void AI()
    {
        if (!Reverse)
        {
            if (beam == null || beam._disposed)
                beam = new(WidthFunct, ColorFunct, null, 20);

            Vector2 start = Projectile.Center;
            Vector2 end = start - Vector2.UnitY * Animators.MakePoly(3f).InOutFunction.Evaluate(Time, 0f, 1f, 2f, 1400f);
            points.SetPoints(start.GetLaserControlPoints(end, 20));
        }

        if (Time >= TotalDuration)
        {
            if (!Reverse)
            {
                if (this.RunLocal())
                {
                    CloudedCrater.ClientWorldDataTag = CloudedCrater.SafeWorldDataToTag("Client", false);
                    if (SubworldSystem.IsActive<CloudedCrater>())
                    {
                        SubworldSystem.Exit();
                    }
                    else
                        SubworldSystem.Enter<CloudedCrater>();
                }
            }

            Time = Reverse ? TotalDuration : 0;
            Projectile.Kill();
            this.Sync();
            return;
        }

        Time += Reverse ? .02f : .01f;
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("AsterlinSpaceTravel");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Shader.TrySetParameter("time", Time);
        Shader.TrySetParameter("reverse", Reverse);
        Shader.TrySetParameter("stop", !Projectile.active);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("AsterlinSpaceTravel", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

    public bool IsEntityActive() => Projectile.active;

    public float WidthFunct(float c)
    {
        c = 1f - c;
        float percentageFromEnd = .1f;
        float thickness = 44f * InverseLerp(TotalDuration, TotalDuration - .3f, Time);
        float transitionPoint = 1f - percentageFromEnd;
        if (c <= transitionPoint)
        {
            return thickness;
        }
        else
        {
            float term = (c - 1f + percentageFromEnd) / percentageFromEnd;
            return thickness * (float)Math.Sqrt(1f - term * term);
        }
    }
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.Cyan * InverseLerp(0f, 1.4f, Time);

    public OptimizedPrimitiveTrail beam;
    public ManualTrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        if (this.RunLocal())
        {
            if (!HasShader)
                InitializeShader();

            UpdateShader();
        }

        if (!Reverse)
        {
            void draw()
            {
                if (beam == null || points == null)
                    return;

                ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1, SamplerState.AnisotropicWrap);
                beam.DrawTrail(shader, points.Points, 200, true, true);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverNPCs);
        }

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return true;
    }
}