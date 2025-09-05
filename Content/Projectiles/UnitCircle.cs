using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles;

/// <summary>
/// I cannot make this up my adv. pre-cal teacher allowed me to do this for a project so im taking my chance
/// </summary>
public class UnitCircle : ModProjectile
{
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 90000;
    }

    // Set basic properties
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.scale = 0f;
    }

    // Define the owner
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[2];
    public override void AI()
    {
        // Set position
        if (Time == 0f)
            Projectile.Center = Owner.Additions().mouseWorld;

        // Increment timer
        Time++;

        // Set the scale and opacity
        Projectile.scale = Projectile.Opacity = MakePoly(4).InFunction(InverseLerp(0f, 35f, Time));

        // So as to it doesn't last forever
        if (Owner.Additions().MouseRight.Current)
            Projectile.Kill();

        // Otherwise just don't die
        Projectile.timeLeft = 2;
    }

    /// <summary>
    /// 360 in this will just be 0, but slightly below
    /// </summary>
    private readonly List<int> Degrees =
        [330, 315, 300, 270, 240, 225, 210, 180, 150, 135, 120, 90, 60, 45, 30, 0];

    /// <summary>
    /// Draws text to the screen
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="txt"></param>
    /// <param name="center"></param>
    /// <param name="rot"></param>
    /// <param name="color"></param>
    private static void DrawText(SpriteBatch sb, string txt, Vector2 center, float rot, Color color)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 finalPos = center;
        ChatManager.DrawColorCodedStringWithShadow(sb, font, txt, finalPos, color, rot, Vector2.Zero, new Vector2(1f), -1f, 2f);
    }
    public override string Texture => AssetRegistry.Invis;
    private const int RayCount = 16;

    public override bool PreDraw(ref Color lightColor)
    {
        SpriteBatch sb = Main.spriteBatch;
        const float scale = 900f;

        Main.spriteBatch.EnterShaderRegion();
        Texture2D telegraphBase = AssetRegistry.InvisTex;
        ManagedShader circle = ShaderRegistry.CircularAoETelegraph;
        circle.TrySetParameter("opacity", 1f);
        float interpolant = MathF.Pow(Sin01(Main.GlobalTimeWrappedHourly * 2f), 2);
        circle.TrySetParameter("color", (Color.Lerp(Color.DarkRed, Color.Red, interpolant)));
        circle.TrySetParameter("secondColor", Color.Lerp(Color.DarkOrange, Color.OrangeRed, .5f));
        circle.Render();

        Main.EntitySpriteDraw(telegraphBase, Projectile.Center - Main.screenPosition, null, lightColor, 0f, telegraphBase.Size() / 2f, scale, 0, 0f);
        Main.spriteBatch.ExitShaderRegion();

        for (int i = 0; i < Degrees.Count; i++)
        {
            // Count the other way
            float rot = -MathHelper.ToRadians(Degrees[i]);

            // Create the lines
            ManualTrailPoints points = new(10);
            points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + PolarVector(scale / 2, rot), 10));
            OptimizedPrimitiveTrail trail = new(c => 6f, (c, pos) => Color.IndianRed * .6f, null, 10);
            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points, -1, false, false);

            float textRot = 0f;

            #region Text
            float xDist = 200f;
            Vector2 pos = Projectile.Center + (rot.ToRotationVector2() * xDist) - Main.screenPosition;

            // Degrees
            DrawText(sb, $"{Degrees[i]}°", pos, textRot, Color.PaleGoldenrod);

            xDist = 300f;
            pos = Projectile.Center + (rot.ToRotationVector2() * xDist) - Main.screenPosition;

            // redcode type stuff but i don think there is an easier way to manifest the specific numbers

            // Radians
            switch (i + 1)
            {
                case 16:
                    DrawText(sb, $"2pi", pos, textRot, new(177, 235, 110));
                    break;
                case 15:
                    DrawText(sb, $"pi/6", pos, textRot, new(177, 235, 110));
                    break;
                case 14:
                    DrawText(sb, $"pi/4", pos, textRot, new(177, 235, 110));
                    break;
                case 13:
                    DrawText(sb, $"pi/3", pos, textRot, new(177, 235, 110));
                    break;
                case 12:
                    DrawText(sb, $"pi/2", pos, textRot, new(177, 235, 110));
                    break;
                case 11:
                    DrawText(sb, $"2pi/3", pos, textRot, new(177, 235, 110));
                    break;
                case 10:
                    DrawText(sb, $"3pi/4", pos, textRot, new(177, 235, 110));
                    break;
                case 9:
                    DrawText(sb, $"5pi/6", pos, textRot, new(177, 235, 110));
                    break;
                case 8:
                    DrawText(sb, $"pi", pos, textRot, new(177, 235, 110));
                    break;
                case 7:
                    DrawText(sb, $"7pi/6", pos, textRot, new(177, 235, 110));
                    break;
                case 6:
                    DrawText(sb, $"5pi/4", pos, textRot, new(177, 235, 110));
                    break;
                case 5:
                    DrawText(sb, $"4pi/3", pos, textRot, new(177, 235, 110));
                    break;
                case 4:
                    DrawText(sb, $"3pi/2", pos, textRot, new(177, 235, 110));
                    break;
                case 3:
                    DrawText(sb, $"5pi/3", pos, textRot, new(177, 235, 110));
                    break;
                case 2:
                    DrawText(sb, $"7pi/4", pos, textRot, new(177, 235, 110));
                    break;
                case 1:
                    DrawText(sb, $"11pi/6", pos, textRot, new(177, 235, 110));
                    break;
            }

            xDist = 500f;
            pos = Projectile.Center + (rot.ToRotationVector2() * xDist) - Main.screenPosition;

            // Tangent Values
            switch (i + 1)
            {
                case 16:
                    DrawText(sb, $"t = 0", pos, textRot, new(108, 143, 224));
                    break;
                case 15:
                    DrawText(sb, $"t = √3/3", pos, textRot, new(108, 143, 224));
                    break;
                case 14:
                    DrawText(sb, $"t = 1", pos, textRot, new(108, 143, 224));
                    break;
                case 13:
                    DrawText(sb, $"t = √3", pos, textRot, new(108, 143, 224));
                    break;
                case 12:
                    DrawText(sb, $"t = undef.", pos, textRot, new(108, 143, 224));
                    break;
                case 11:
                    DrawText(sb, $"t = -√3", pos, textRot, new(108, 143, 224));
                    break;
                case 10:
                    DrawText(sb, $"t = -1", pos, textRot, new(108, 143, 224));
                    break;
                case 9:
                    DrawText(sb, $"t = -√3/3", pos, textRot, new(108, 143, 224));
                    break;
                case 8:
                    DrawText(sb, $"t = 0", pos, textRot, new(108, 143, 224));
                    break;
                case 7:
                    DrawText(sb, $"t = √3/3", pos, textRot, new(108, 143, 224));
                    break;
                case 6:
                    DrawText(sb, $"t = 1", pos, textRot, new(108, 143, 224));
                    break;
                case 5:
                    DrawText(sb, $"t = √3", pos, textRot, new(108, 143, 224));
                    break;
                case 4:
                    DrawText(sb, $"t = undef.", pos, textRot, new(108, 143, 224));
                    break;
                case 3:
                    DrawText(sb, $"t = -√3", pos, textRot, new(108, 143, 224));
                    break;
                case 2:
                    DrawText(sb, $"t = -1", pos, textRot, new(108, 143, 224));
                    break;
                case 1:
                    DrawText(sb, $"t = -√3/3", pos, textRot, new(108, 143, 224));
                    break;
            }

            xDist = scale * .5f;
            pos = Projectile.Center + (rot.ToRotationVector2() * xDist) - Main.screenPosition;

            // Coordinates
            switch (i + 1)
            {
                case 16:
                    DrawText(sb, $"(1, 0)", pos, textRot, new(164, 86, 209));
                    break;
                case 15:
                    DrawText(sb, $"(√3/2, 1/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 14:
                    DrawText(sb, $"(√2/2, √2/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 13:
                    DrawText(sb, $"(1/2, √3/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 12:
                    DrawText(sb, $"(0, 1)", pos, textRot, new(164, 86, 209));
                    break;
                case 11:
                    DrawText(sb, $"(-1/2, √3/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 10:
                    DrawText(sb, $"(-√2/2, √2/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 9:
                    DrawText(sb, $"(-√3/2, 1/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 8:
                    DrawText(sb, $"(-1, 0)", pos, textRot, new(164, 86, 209));
                    break;
                case 7:
                    DrawText(sb, $"(-√3/2, -1/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 6:
                    DrawText(sb, $"(-√2/2, -√2/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 5:
                    DrawText(sb, $"(-1/2, -√3/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 4:
                    DrawText(sb, $"(0, -1)", pos, textRot, new(164, 86, 209));
                    break;
                case 3:
                    DrawText(sb, $"(1/2, -√3/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 2:
                    DrawText(sb, $"(√2/2, -√2/2)", pos, textRot, new(164, 86, 209));
                    break;
                case 1:
                    DrawText(sb, $"(√3/2, -1/2)", pos, textRot, new(164, 86, 209));
                    break;
            }

            float floaty = MathF.Pow(Cos01(Main.GlobalTimeWrappedHourly * .9f), 2);
            pos = Projectile.Center + new Vector2(-scale + 100f, -300f + (floaty * 40f)) - Main.screenPosition;
            DrawText(sb, $"Purple is Coordinates", pos, textRot, Color.White);
            pos = Projectile.Center + new Vector2(-scale + 100f, -270f + (floaty * 40f)) - Main.screenPosition;
            DrawText(sb, $"Blue is Tangent values", pos, textRot, Color.White);
            pos = Projectile.Center + new Vector2(-scale + 100f, -240f + (floaty * 40f)) - Main.screenPosition;
            DrawText(sb, $"Green is for Radians", pos, textRot, Color.White);
            pos = Projectile.Center + new Vector2(-scale + 100f, -210f + (floaty * 40f)) - Main.screenPosition;
            DrawText(sb, $"Yellow is for Degrees", pos, textRot, Color.White);

            #endregion Text
        }
        return false;
    }
}
