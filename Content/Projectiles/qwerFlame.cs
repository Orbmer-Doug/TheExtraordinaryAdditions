using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles;

public class qwerFlame : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 3;
        Projectile.scale = 0f;
    }

    public ref float Timer => ref Projectile.ai[0];
    public const int Lifetime = 150;
    public override void AI()
    {
        Projectile.scale = Projectile.Opacity = Utils.GetLerpValue(0f, Lifetime, Timer, true);
        Timer++;

        Vector2 vel = Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.4f, 0.4f);

        Color col = Color.Gray * Projectile.Opacity;

        Vector2 pos = Projectile.Center;
        Color color = GetFireColors();
        Lighting.AddLight(Projectile.Center, color.ToVector3() * Projectile.scale * 2);

        Projectile.timeLeft = 4;
        Projectile.Center = Main.MouseWorld;
        if (Main.LocalPlayer.Additions().MouseRight.Current)
            Projectile.Kill();

        for (int i = 0; i < 8; i++)
        {
            float scale = Projectile.scale * Main.rand.NextFloat(.9f, 1.1f);
            int life = Main.rand.Next(20, 30);
            //ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale + .3f, color, .5f, Projectile.scale < .9f);
        }
    }

    private Color GetFireColors()
    {
        // Truly a propane moment
        Color smoke = Color.DarkGray;
        Color kel1000 = new(255, 50, 0);
        Color kel2000 = new(255, 124, 0);
        Color kel3000 = new(246, 190, 33);
        Color kel4000 = new(255, 221, 80);
        Color kel5000 = new(246, 243, 192);
        Color kel6000 = new(214, 249, 251);
        Color kel7000 = new(108, 226, 254);
        Color kel8000 = new(5, 205, 255);
        Color kel9000 = new(4, 139, 254);
        Color kel10000 = new(0, 77, 255);

        Color[] spectrum = [Color.Transparent, smoke, kel1000, kel2000, kel3000, kel4000, kel5000, Color.White, kel6000, kel7000, kel8000, kel9000, kel10000];
        float interpolant = 1f - Projectile.scale;

        return MulticolorLerp(interpolant, spectrum);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.TheGiantSnailFromAncientTimes);
        Main.spriteBatch.EnterShaderRegion();
        ManagedShader fade = AssetRegistry.GetShader("StygainDisintegration");
        fade.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise), 1);
        fade.TrySetParameter("opacity", Sin01(Main.GlobalTimeWrappedHourly));
        fade.Render();

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Projectile.GetAlpha(lightColor), 0f, tex.Size() / 2, 1f, 0);

        Main.spriteBatch.ExitShaderRegion();

        return false;
    }
}

public class qwerik : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 50000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.hostile = Projectile.friendly = Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 3600;
        Projectile.timeLeft *= 5;
    }

    // i dont actually want to make a atlas system but i dont want 19 images in the asset folder
    public static readonly Rectangle headRect = new(0, 0, 46, 60);
    public static readonly Rectangle bodyRect = new(46, 0, 104, 156);

    public static readonly Rectangle leftAngledBackLimbRect = new(150, 0, 24, 18);
    public static readonly Rectangle leftAngledForeLimbRect = new(174, 0, 26, 28);
    public static readonly Rectangle leftAngledHandRect = new(200, 0, 26, 28);

    public static readonly Rectangle rightAngledBackLimbRect = new(150, 30, 28, 28);
    public static readonly Rectangle rightAngledForeLimbRect = new(178, 30, 22, 30);
    public static readonly Rectangle rightAngledHandRect = new(200, 30, 24, 26);

    public static readonly Rectangle leftAngledLegRect = new(226, 0, 50, 90);
    public static readonly Rectangle rightAngledLegRect = new(276, 0, 46, 90);

    public static readonly Rectangle straightBodyRect = new(0, 156, 110, 192);

    public static readonly Rectangle leftStraightBackLimbRect = new(110, 156, 28, 30);
    public static readonly Rectangle leftStraightForeLimbRect = new(138, 156, 20, 30);
    public static readonly Rectangle leftStraightHandRect = new(158, 156, 16, 28);

    public static readonly Rectangle rightStraightBackLimbRect = new(110, 186, 28, 30);
    public static readonly Rectangle rightStraightForeLimbRect = new(138, 186, 20, 32);
    public static readonly Rectangle rightStraightHandRect = new(158, 186, 16, 26);

    public static readonly Rectangle leftStraightLegRect = new(174, 156, 40, 92);
    public static readonly Rectangle rightStraightLegRect = new(214, 156, 40, 92);

    public Vector2 EyePos;
    public Vector2 LeftHandPos;
    public Vector2 RightHandPos;
    public Vector2 LeftFootPos;
    public Vector2 RightFootPos;

    public Vector2 LeftVentPos;
    public Vector2 RightVentPos;

    public float bodyRot;
    public float leftLegRot;
    public float rightLegRot;
    public float headRot;

    private float _ZPosition;
    /// <summary>
    /// Ranges between .5 - 1 <br></br>
    /// </summary>
    public float ZPosition
    {
        get => _ZPosition;
        set => _ZPosition = Utils.Remap(value, 0f, 1f, .5f, 1f);
    }

    public float EyeGleamInterpolant;

    public bool straight;

    public int direction;

    public int time;

    public bool flipped;

    public bool diving;

    public JointChain leftArm;
    public JointChain rightArm;
    public override void AI()
    {
        Vector2 root = Main.LocalPlayer.Center - Vector2.UnitY * 200f;
        Vector2 target = Main.MouseWorld;
        Projectile.Center = root;
        Projectile.velocity = Main.LocalPlayer.velocity;

        if (!straight)
            direction = -Main.LocalPlayer.direction;// (target.X < root.X).ToDirectionInt();
        else
            direction = 1;

        if (Projectile.ai[0] == 0f)
        {
            leftArm = new JointChain(
                    root,
                    (leftAngledBackLimbRect.Height, null), // Back limb
                    (leftAngledForeLimbRect.Height, null), // Fore limb
                    (leftAngledHandRect.Height, null) // Hand
                );

            rightArm = new JointChain(
                    root,
                    (rightAngledBackLimbRect.Height, null), // Back limb
                    (rightAngledForeLimbRect.Height, null), // Fore limb
                    (rightAngledHandRect.Height, null) // Hand
                );

            straight = false;
            //QueueDirectionChange();

            Projectile.ai[0] = 1f;
        }

        ZPosition = 1f;//Sin01(Main.GlobalTimeWrappedHourly);
        EyeGleamInterpolant = Sin01(Main.GlobalTimeWrappedHourly);
        UpdateGraphics();

        Projectile.timeLeft = 2;
        if (Main.LocalPlayer.Additions().MouseMiddle.JustPressed)
            Projectile.Kill();

        time++;
    }

    public void UpdateGraphics()
    {
        Vector2 root = Main.LocalPlayer.Center - Vector2.UnitY * 300f;
        Vector2 target = Main.LocalPlayer.Additions().mouseWorld;

        flipped = false;
        diving = true;
        bodyRot = Projectile.Center.AngleTo(Main.MouseWorld) + MathHelper.PiOver2;
        if (!diving)
        {
            if (direction == 1 && float.IsNegative(MathF.Cos(bodyRot)))
            {
                bodyRot += Pi;
                flipped = true;
            }
        }

        List<CCDKinematicJoint> leftJoints = leftArm.Root.GetSubLimb();
        List<CCDKinematicJoint> rightJoints = rightArm.Root.GetSubLimb();

        // 0 is the root
        // 1 is the back limb
        // 2 is the fore limb
        // 3 is the hand
        if (!straight) // Angled
        {
            leftJoints[0].JointLength = leftAngledBackLimbRect.Height * ZPosition;
            rightJoints[0].JointLength = rightAngledBackLimbRect.Height * ZPosition;
            leftJoints[1].JointLength = leftAngledBackLimbRect.Height * ZPosition;
            rightJoints[1].JointLength = rightAngledBackLimbRect.Height * ZPosition;
            leftJoints[2].JointLength = leftAngledForeLimbRect.Height * ZPosition;
            rightJoints[2].JointLength = rightAngledForeLimbRect.Height * ZPosition;
            leftJoints[3].JointLength = leftAngledHandRect.Height * ZPosition;
            rightJoints[3].JointLength = rightAngledHandRect.Height * ZPosition;
        }
        else
        {
            leftJoints[0].JointLength = leftStraightBackLimbRect.Height * ZPosition;
            rightJoints[0].JointLength = rightStraightBackLimbRect.Height * ZPosition;
            leftJoints[1].JointLength = leftStraightBackLimbRect.Height * ZPosition;
            rightJoints[1].JointLength = rightStraightBackLimbRect.Height * ZPosition;
            leftJoints[2].JointLength = leftStraightForeLimbRect.Height * ZPosition;
            rightJoints[2].JointLength = rightStraightForeLimbRect.Height * ZPosition;
            leftJoints[3].JointLength = leftStraightHandRect.Height * ZPosition;
            rightJoints[3].JointLength = rightStraightHandRect.Height * ZPosition;
        }

        Vector2 leftRoot = straight ? Projectile.Center + PolarVector(13f * ZPosition, bodyRot - PiOver2) + PolarVector(44f * ZPosition * direction, bodyRot + Pi) :
            Projectile.Center + PolarVector(26f * ZPosition, bodyRot - PiOver2) + PolarVector(42f * direction * ZPosition, bodyRot + Pi);
        leftArm.RootPosition = leftRoot + Projectile.velocity;
        float leftHandRot = leftJoints[2].Position.AngleTo(leftJoints[3].Position) - PiOver2;
        Vector2 leftHandOffset = straight ? PolarVector(4f * ZPosition * direction, leftHandRot + Pi) :
            PolarVector(11f * ZPosition * direction, leftHandRot + Pi) + PolarVector(8f * ZPosition, leftHandRot - PiOver2);
        leftArm.Update(target - leftHandOffset, 50);

        Vector2 rightRoot = straight ? Projectile.Center + PolarVector(13f * ZPosition, bodyRot - PiOver2) + PolarVector(46f * ZPosition * direction, bodyRot) :
            Projectile.Center + PolarVector(33f * ZPosition, bodyRot - PiOver2) + PolarVector(42f * direction * ZPosition, bodyRot);
        rightArm.RootPosition = rightRoot + Projectile.velocity;
        float rightHandRot = rightJoints[2].Position.AngleTo(rightJoints[3].Position) - PiOver2;
        Vector2 rightHandOffset = PolarVector(6f * direction, rightHandRot);
        rightArm.Update(target - rightHandOffset, 50);

        LeftHandPos = leftJoints[2].Position + leftHandOffset + PolarVector(leftAngledHandRect.Height, leftHandRot + PiOver2);
        RightHandPos = rightJoints[2].Position + rightHandOffset + PolarVector(rightAngledHandRect.Height, rightHandRot + PiOver2);

        LeftVentPos = straight ? Projectile.Center + PolarVector(18f * ZPosition * direction, bodyRot + Pi) + PolarVector(5f * ZPosition, bodyRot - PiOver2) :
            Projectile.Center + PolarVector(28f * ZPosition * direction, bodyRot + Pi) + PolarVector(26f * ZPosition, bodyRot - PiOver2);
        RightVentPos = straight ? Projectile.Center + PolarVector(18f * ZPosition * direction, bodyRot) + PolarVector(5f * ZPosition, bodyRot - PiOver2) :
            Projectile.Center + PolarVector(26f * ZPosition, bodyRot - PiOver2);

        EyePos = straight ? Projectile.Center + PolarVector(52f, bodyRot - PiOver2) :
            Projectile.Center + PolarVector(62f * ZPosition, bodyRot - PiOver2) + PolarVector(2f * ZPosition * direction, bodyRot) + PolarVector(18f * ZPosition * -direction, (-.91f * -direction) + headRot);

        headRot = EyePos.AngleTo(target);
        if (direction == 1 && !straight)
            headRot += Pi;

        float velRot = Vector2.Dot(Projectile.velocity, bodyRot.ToRotationVector2()) * .03f;
        leftLegRot = leftLegRot.AngleLerp(velRot, .2f);
        rightLegRot = rightLegRot.AngleLerp(velRot, .2f);

        float leftLegAdd = (straight ? leftStraightLegRect.Height : leftAngledLegRect.Height);
        Vector2 leftShift = PolarVector(leftLegAdd * ZPosition, (bodyRot + leftLegRot) + PiOver2);
        Vector2 leftLegPos = straight ? Projectile.Center + PolarVector(90f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot + Pi) :
            Projectile.Center + PolarVector(68f * ZPosition, bodyRot + PiOver2) + PolarVector(27f * ZPosition * direction, bodyRot + Pi);

        LeftFootPos = leftLegPos + leftShift + Projectile.velocity;

        float rightLegAdd = (straight ? rightStraightLegRect.Height : rightAngledLegRect.Height);
        Vector2 rightShift = PolarVector(rightLegAdd * ZPosition, (bodyRot + rightLegRot) + PiOver2);
        Vector2 rightLegPos = straight ? Projectile.Center + PolarVector(90f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot) :
            Projectile.Center + PolarVector(68f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot);

        RightFootPos = rightLegPos + rightShift + Projectile.velocity;

        if (leftLegFlame == null || leftLegFlame._disposed)
            leftLegFlame = new(FlameWidthFunct, FlameColorFunct, null, 30);
        if (rightLegFlame == null || rightLegFlame._disposed)
            rightLegFlame = new(FlameWidthFunct, FlameColorFunct, null, 30);

        OldPositions.Update(Projectile.Center);

        float dist = MathHelper.Lerp(110f, 200f, AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f);
        for (int i = 0; i < leftLegPoints.Points.Length; i++)
        {
            Vector2 oldPosition = OldPositions.Points[i] + (LeftFootPos - Projectile.Center);
            leftLegPoints.SetPoint(i, Vector2.Lerp(OldPositions.Points[i] + (LeftFootPos - Projectile.Center), LeftFootPos, .5f)
                - PolarVector(1f, ((bodyRot + leftLegRot) - MathHelper.PiOver2)) * i / (float)(leftLegPoints.Points.Length - 1f) * dist);
            rightLegPoints.SetPoint(i, Vector2.Lerp(OldPositions.Points[i] + (RightFootPos - Projectile.Center), RightFootPos, .5f)
                - PolarVector(1f, ((bodyRot + rightLegRot) - MathHelper.PiOver2)) * i / (float)(rightLegPoints.Points.Length - 1f) * dist);
        }
    }

    public void DrawLeftArm(Texture2D atlas, Color color, SpriteEffects flip)
    {
        if (leftArm != null)
        {
            // Collect all joints in the chain
            List<CCDKinematicJoint> joints = leftArm.Root.GetSubLimb();

            float backLimbRot = joints[0].Position.AngleTo(joints[1].Position) - PiOver2;
            Rectangle backLimbSource = straight ? leftStraightBackLimbRect : leftAngledBackLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[0].Position,
                backLimbSource, color, backLimbRot, new(backLimbSource.Width / 2f, 0f), ZPosition, flip);

            float foreLimbRot = joints[1].Position.AngleTo(joints[2].Position) - PiOver2;
            Rectangle foreLimbSource = straight ? leftStraightForeLimbRect : leftAngledForeLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[1].Position - PolarVector(4f * direction, foreLimbRot),
                foreLimbSource, color, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), ZPosition, flip);

            float handRot = joints[2].Position.AngleTo(joints[3].Position) - PiOver2;
            Rectangle handSource = straight ? leftStraightHandRect : leftAngledHandRect;
            Main.spriteBatch.DrawBetter(atlas, joints[2].Position + (straight ? PolarVector(4f * ZPosition * direction, handRot + Pi) : PolarVector(11f * ZPosition * direction, handRot + Pi) + PolarVector(8f * ZPosition, handRot - PiOver2)),
                handSource, color, handRot, new(handSource.Width / 2, 0f), ZPosition, flip);
            #region Test
            /*
            // Draw each segment as a red rectangle
            for (int i = 0; i < joints.Count - 1; i++)
            {
                Vector2 start = joints[i].Position;
                Vector2 end = joints[i + 1].Position;
                float length = Vector2.Distance(start, end);
                float angle = start.AngleTo(end);

                Main.spriteBatch.Draw(
                    AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                    start - Main.screenPosition,
                    null,
                    Color.Red.Lerp(Color.Blue, InverseLerp(0f, joints.Count, i)),
                    angle,
                    new Vector2(0, 0.5f), // Origin at left center of pixel
                    new Vector2(length, 5f), // Scale to segment length and thickness
                    SpriteEffects.None,
                    0
                );
            }
            */
            #endregion
        }
    }

    public void DrawRightArm(Texture2D atlas, Color color, SpriteEffects flip)
    {
        if (rightArm != null)
        {
            List<CCDKinematicJoint> joints = rightArm.Root.GetSubLimb();

            float backLimbRot = joints[0].Position.AngleTo(joints[1].Position) - PiOver2;
            Rectangle backLimbSource = straight ? rightStraightBackLimbRect : rightAngledBackLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[0].Position,
                backLimbSource, color, backLimbRot, new(backLimbSource.Width / 2f, 0f), ZPosition, flip);

            float foreLimbRot = joints[1].Position.AngleTo(joints[2].Position) - PiOver2;
            Rectangle foreLimbSource = straight ? rightStraightForeLimbRect : rightAngledForeLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[1].Position + PolarVector(5f * direction, foreLimbRot),
                foreLimbSource, color, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), ZPosition, flip);

            float handRot = joints[2].Position.AngleTo(joints[3].Position) - PiOver2;
            Rectangle handSource = straight ? rightStraightHandRect : rightAngledHandRect;
            Main.spriteBatch.DrawBetter(atlas, joints[2].Position + PolarVector(6f * direction, handRot),
                handSource, color, handRot, new(handSource.Width / 2, 0f), ZPosition, flip);
        }
    }

    public void DrawBody(Texture2D atlas, Color color, SpriteEffects flip)
    {
        Rectangle bodySource = straight ? straightBodyRect : bodyRect;
        Main.spriteBatch.DrawBetter(atlas, Projectile.Center, bodySource, color, bodyRot, bodySource.Size() / 2f, ZPosition, flip);
    }

    public void DrawHead(Texture2D atlas, Color color, SpriteEffects flip)
    {
        if (!straight)
            Main.spriteBatch.DrawBetter(atlas, Projectile.Center + PolarVector(60f * ZPosition, bodyRot - PiOver2) + PolarVector(2f * ZPosition * direction, bodyRot),
            headRect, color, headRot, new Vector2(headRect.Width / 2, headRect.Height), ZPosition, flip);

        if (EyeGleamInterpolant > 0f)
        {
            void gleam()
            {
                Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Vector2 scale = new(Animators.MakePoly(4f).OutFunction.Evaluate(0f, 400f, EyeGleamInterpolant), Animators.MakePoly(2f).InOutFunction.Evaluate(0f, 50f, EyeGleamInterpolant));
                float rot = bodyRot;
                Main.spriteBatch.DrawBetterRect(star, ToTarget(EyePos + Projectile.velocity, scale * .4f), null, Color.LightCyan, rot, star.Size() / 2);
                Main.spriteBatch.DrawBetterRect(star, ToTarget(EyePos + Projectile.velocity, scale), null, Color.Cyan, rot, star.Size() / 2);
            }
            PixelationSystem.QueueTextureRenderAction(gleam, PixelationLayer.OverProjectiles, BlendState.Additive);
        }
    }

    public TrailPoints OldPositions = new(30);

    public ManualTrailPoints leftLegPoints = new(30);
    public OptimizedPrimitiveTrail leftLegFlame;

    public ManualTrailPoints rightLegPoints = new(30);
    public OptimizedPrimitiveTrail rightLegFlame;

    public float FlameWidthFunct(float completionRatio) => MathHelper.SmoothStep(32f, 8f, Animators.MakePoly(1.4f).OutFunction(completionRatio)) * Projectile.scale;

    public Color FlameColorFunct(SystemVector2 completionRatio, Vector2 pos)
    {
        Color startingColor = Color.Lerp(Color.LightSkyBlue, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.DarkBlue, Color.Cyan, 0.2f);
        Color endColor = Color.Lerp(Color.DarkCyan, Color.Cyan, 0.67f);
        return MulticolorLerp(completionRatio.X, startingColor, middleColor, endColor) * GetLerpBump(0f, .1f, .8f, .27f, completionRatio.X) * Projectile.Opacity;
    }

    public void DrawLegs(Texture2D atlas, Color color, SpriteEffects flip)
    {
        Vector2 leftLegPos = straight ? Projectile.Center + PolarVector(90f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot + Pi) :
            Projectile.Center + PolarVector(68f * ZPosition, bodyRot + PiOver2) + PolarVector(27f * ZPosition * direction, bodyRot + Pi);
        Vector2 rightLegPos = straight ? Projectile.Center + PolarVector(90f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot) :
            Projectile.Center + PolarVector(68f * ZPosition, bodyRot + PiOver2) + PolarVector(21f * ZPosition * direction, bodyRot);
        Rectangle leftLegSource = straight ? leftStraightLegRect : leftAngledLegRect;
        Rectangle rightLegSource = straight ? rightStraightLegRect : rightAngledLegRect;
        Main.spriteBatch.DrawBetter(atlas, leftLegPos,
            leftLegSource, color, leftLegRot + bodyRot, new Vector2(leftLegSource.Width / 2, 0f), ZPosition, flip);
        Main.spriteBatch.DrawBetter(atlas, rightLegPos,
            rightLegSource, color, rightLegRot + bodyRot, new Vector2(rightLegSource.Width / 2, 0f), ZPosition, flip);

        void flame()
        {
            ManagedShader shader = ShaderRegistry.SmoothFlame;
            shader.TrySetParameter("heatInterpolant", 1.9f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);

            if (leftLegFlame != null && leftLegPoints != null)
            {
                leftLegFlame.DrawTrail(shader, leftLegPoints.Points, 200, true, true);
            }

            if (rightLegFlame != null && rightLegPoints != null)
            {
                rightLegFlame.DrawTrail(shader, rightLegPoints.Points, 200, true, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(flame, PixelationLayer.OverNPCs, null);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D atlas = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlas);
        SpriteEffects flip = straight ? SpriteEffects.None : (direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        Color color = Color.White.Lerp(Color.Gray, 1f - ZPosition) * Projectile.Opacity;

        if (!straight)
        {
            DrawLeftArm(atlas, color, flip);
        }

        DrawBody(atlas, color, flip);
        DrawHead(atlas, color, flip);

        if (straight)
        {
            DrawLeftArm(atlas, color, flip);
            DrawRightArm(atlas, color, flip);
        }
        else
            DrawRightArm(atlas, color, flip);

        DrawLegs(atlas, color, flip);
        return false;
    }
}

public class qwerproj : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.timeLeft = 400;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    float time;
    float ai;
    public override void AI()
    {
        Player p = Main.LocalPlayer;
        Vector2 target = p.Additions().mouseWorld;
        Projectile.Center = p.Center - Vector2.UnitY * 300f;
        Projectile.velocity = Projectile.Center.SafeDirectionTo(target);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.timeLeft = 2;
        if (p.Additions().MouseMiddle.JustPressed)
            Projectile.Kill();

        ai++;

        /*
        if (ai % 30 == 29)
        {
            bool extra = ai % 60 == 59;
            bool predict = ai % 120 == 119;
            bool throwOff = ai % 180 == 179;
            int count = throwOff ? 4 : predict ? 1 : extra ? 3 : 2;
            float maxAngle = throwOff ? .9f : extra ? .5f : (.2f + Main.rand.NextFloat(-.04f, .04f));
            for (int i = 0; i < count; i++)
            {
                Vector2 dir;
                if (!predict)
                {
                    float angle = Utils.Remap(i, 0f, count - 1, -maxAngle, maxAngle);// MathHelper.Lerp(-maxAngle, maxAngle, i / (count - 1));
                    float angleToTarget = Projectile.Center.AngleTo(p.Center);
                    dir = (angleToTarget + angle).ToRotationVector2();
                }
                else
                    dir = Projectile.Center.SafeDirectionTo(p.Center + p.velocity * 20f);

                AdditionsDebug.DebugLine(Projectile.Center, Projectile.Center + dir * 500f, Color.Red, 5, 20);
            }
        }
        */


        time += .01f;
    }

    /*
    // Hemisphere (rounded tip)
    public float WidthFunct(float c)
    {
        float percentageFromEnd = .4f;
        float thickness = 44f; // Constant thickness (4 units total, 2 per side)
        float transitionPoint = 1f - percentageFromEnd; // Start of hemispherical taper
        if (c <= transitionPoint)
        {
            return thickness;
        }
        else
        {
            float term = (c - 1f + percentageFromEnd) / percentageFromEnd;
            return thickness * (float)Math.Sqrt(1f - term * term);
            
            // Use in place of above to bulge inwards into a point
            // float term = (1f - completionRatio) / percentageFromEnd;
            // return constantWidth * term * term;
        }
    }


    Alternatively:
        float tipInterpolant = MathF.Sqrt(1f - MathF.Pow(Utils.GetLerpValue(0.3f, 0f, 1f - c, true), 2f));
        float width = Utils.GetLerpValue(1f, 0.4f, 1f - c, true) * tipInterpolant * Projectile.scale;


    */

    /*
     * Leaf.
     *  MathF.Sin(MathHelper.Pi * Animators.MakePoly(4f).OutFunction(c))
     */

    public float WidthFunct(float c)
    {

        float tipInterpolant = MathF.Sqrt(1f - MathF.Pow(Utils.GetLerpValue(0.3f, 0f, 1f - c, true), 2f));
        float width = Utils.GetLerpValue(1f, 0.4f, 1f - c, true) * tipInterpolant * Projectile.scale;
        return MathHelper.Lerp(40f, 75f, c);

        float percentageFromEnd = .4f;
        float thickness = 44f; // Constant thickness (4 units total, 2 per side)
        float transitionPoint = 1f - percentageFromEnd; // Start of hemispherical taper

        if (c <= transitionPoint)
        {
            return MathHelper.Lerp(0f, thickness + 2f, MathHelper.Lerp(0f, 1f - percentageFromEnd, c / percentageFromEnd));
        }
        else
        {
            float term = (c - 1f + percentageFromEnd) / percentageFromEnd;
            return thickness * (float)Math.Sqrt(1f - term * term);
        }
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return MulticolorLerp(c.X, Color.Goldenrod, Color.Orange, Color.DarkOrange);
    }

    public float FlameEngulfWidthFunction(float c)
    {
        float baseWidth = MathHelper.Lerp(114f, 50f, c);
        float tipSmoothenFactor = MathF.Sqrt(1f - InverseLerp(0.3f, 0.015f, c).Cubed());
        return baseWidth * tipSmoothenFactor;
    }

    public Color FlameEngulfColorFunction(SystemVector2 c, Vector2 pos)
    {
        Color flameTipColor = new(196, 240, 255);
        Color limeFlameColor = new(125, 222, 255);
        Color greenFlameColor = new(31, 198, 255);
        Color trailColor = MulticolorLerp(Animators.MakePoly(2.45f).OutFunction(c.X) * 0.7f, flameTipColor, limeFlameColor, greenFlameColor);
        return trailColor * (1 - c.X) * 1f;
    }

    public TrailPoints points = new(25);
    public override bool PreDraw(ref Color lightColor)
    {
        /*
        void draw()
        {
            OptimizedPrimitiveTrail trail = new(FlameEngulfWidthFunction, FlameEngulfColorFunction, null, 100);
            ManualTrailPoints points = new(100);
            points.SetPoints(Main.LocalPlayer.Center.GetLaserControlPoints(Main.LocalPlayer.Additions().mouseWorld, 100));

            if (trail == null || points == null)
                return;

            ManagedShader shader = AssetRegistry.GetShader("FlameEngulfShader");
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1, SamplerState.AnisotropicWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 2, SamplerState.AnisotropicWrap);
            shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 1.2f);
            trail.DrawTrail(shader, points.Points, 200, true, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverNPCs);
        
        */
        /*
        Vector2 center = Main.LocalPlayer.Center + Vector2.UnitX * 600f;
        Vector2 entrance = center - Vector2.UnitY * 150f;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Asterlin_BossChecklist);
        ManagedShader shader = AssetRegistry.GetShader("PortalAppearShader");
        shader.TrySetParameter("LinePoint", entrance - Main.screenPosition);
        shader.TrySetParameter("LineDir", Vector2.UnitX);
        shader.TrySetParameter("FadeDistance", 20f);
        shader.TrySetParameter("NoiseColor", Color.DeepSkyBlue.ToVector4());
        shader.TrySetParameter("Resolution", tex.Size());
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
        Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader.Effect);
        shader.Render();
        Main.spriteBatch.DrawBetter(tex, Main.MouseWorld, null, Color.White, 0f, tex.Size() / 2, 1f);
        Main.spriteBatch.ExitShaderRegion();

        ManagedShader pShader = AssetRegistry.GetShader("PortalShaderAlt");
        pShader.TrySetParameter("scale", 1f);
        pShader.TrySetParameter("coolColor", Color.DarkCyan.ToVector3());
        pShader.TrySetParameter("mediumColor", Color.Cyan.ToVector3());
        pShader.TrySetParameter("hotColor", Color.DeepSkyBlue.ToVector3());
        void portal()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            pShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
            pShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseZoomedOut), 2, SamplerState.LinearWrap);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(center, new(700, 250)), null, Color.White, 0, tex.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(portal, PixelationLayer.OverProjectiles, null, pShader);
        */

        /* FIREBALL
        float rad = 200f;
        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(rad)), null, Color.White, 0f, tex.Size() / 2, 0);
        }

        ManagedShader shader = AssetRegistry.GetShader("IntenseFireball");
        shader.TrySetParameter("time", time * 1.2f);
        shader.TrySetParameter("resolution", new Vector2(rad));
        shader.TrySetParameter("opacity", 1f);

        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderPlayers, null, shader);
        */

        /* ASTERLIN POWER
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Asterlin_BossChecklist);
        ManagedShader shader = AssetRegistry.GetShader("AsterlinPower");
        shader.TrySetParameter("time", time * 9.4f);
        shader.TrySetParameter("resolution", tex.Size().Length());

        float rad = 500f;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        shader.Render("AutoloadPass", false, false);
        //Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(rad)), null, Color.White, 0f, tex.Size() / 2, 0);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White, 0f, tex.Size() / 2f, 1f);
        Main.spriteBatch.ExitShaderRegion();
        */

        /* ABYSS WATER
        SpriteBatch sb = Main.spriteBatch;
        float waterInterpolant = 1f;
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        ManagedShader f = ShaderRegistry.OceanLayer;
        f.TrySetParameter("time", Main.GlobalTimeWrappedHourly * .3f);
        f.TrySetParameter("opacity", waterInterpolant * 1f);
        f.TrySetParameter("riseInterpolant", waterInterpolant);
        f.TrySetParameter("causticSpeed", .4f);
        f.TrySetParameter("intensity", 18.8f);

        Main.spriteBatch.EnterShaderRegion(null, f.Effect);
        f.Render();
        sb.Draw(pixel, new Rectangle(Main.screenWidth, Main.screenHeight, Main.screenWidth, Main.screenHeight), null, Color.White, MathHelper.Pi, Vector2.Zero, 0, 0f);

        Main.spriteBatch.ExitShaderRegion();
        */

        points.Update(Main.LocalPlayer.Additions().mouseWorld);

        void draw()
        {
            OptimizedPrimitiveTrail trail = new(c => 200f, (c, pos) => Color.White, null, 100);
            ManagedShader shader = AssetRegistry.GetShader("ConvergingFlameShader");
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 1.2f);
            shader.TrySetParameter("opacity", 1f);
            trail.DrawTrail(shader, points.Points, 400, true, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderPlayers, null);
        return false;
    }
}

// TODO: not only have this system integrated into how dialogue works for asterlin but have a separate system for the pet.
// The pet will be a small screen of asterlin judging you, where its background changes depending on biome/event
// Idea of dialogue is neat, but sounds very cumbersome to account for every condition (and might get stale)
/*
[Autoload(Side = ModSide.Client)]
public class CRTSystem : ModSystem
{
    private ManagedRenderTarget crtTarget;
    private ManagedShader crtShader;
    private static readonly RenderTargetInitializationAction TargetInitializer = (width, height) => new RenderTarget2D(Main.instance.GraphicsDevice, width, height);

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            crtTarget = new ManagedRenderTarget(true, TargetInitializer, true);

            // Initialize target
            GraphicsDevice device = Main.instance.GraphicsDevice;
            device.SetRenderTarget(crtTarget);
            device.Clear(Color.Transparent);
            device.SetRenderTarget(null);
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTarget;
        On_Main.DrawProjectiles += DrawTheTarget;
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            crtTarget?.Dispose();
            crtTarget = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTarget;
        On_Main.DrawProjectiles -= DrawTheTarget;
    }

    public string Key => $"Mods.{Mod.Name}.NPCs.Asterlin.";
    public static readonly Color InfoColor = Color.Goldenrod;
    public static readonly Color WarningColor = Color.Red;
    public static readonly Color StatusColor = Color.DeepSkyBlue;
    public TextSnippet Pause => new(" ", Color.Transparent, .5f);
    public TextSnippet LongPause => new(" ", Color.Transparent, 3.6f);
    public AwesomeSentence FullDialogue => new(1000f, [
        new TextSnippet(Language.GetOrRegister($"{Key}OverheatTemperatureWarning").Format(Main.time), WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatImminent").Format(Main.time), WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatHeatsinks").Format(Main.time), InfoColor, .025f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
        new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan").Format(Main.time), StatusColor, .02f, null, null, true, 2.4f),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan1").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan2").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan3").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans").Format(Main.time), StatusColor, .02f, null, null, true, 2.4f),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans1").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans2").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans3").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans").Format(Main.time), StatusColor, .02f, null, null, true, 2.4f),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans1").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans2").Format(Main.time), StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(Language.GetOrRegister($"{Key}OverheatUhOh").Format(Main.time), InfoColor, .037f, TextSnippet.AppearFadingFromRight, TextSnippet.RandomDisplacement, true, 2.4f),
        LongPause
    ]);

    public DialogueManager manager;
    public int sep;
    private void DrawToTarget()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        Vector2 resolution = new(Main.screenWidth, Main.screenHeight);
        GlobalPlayer moddedPlayer = Main.LocalPlayer.Additions();
        /*
        AwesomeSentence sent1 = new(1000f, [
        new TextSnippet("ok you gotta be kidding with this its been 2 YEARS at this point making TEA?", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("and you seriously cant even", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet(" finish ", Color.Gold, .085f, TextSnippet.AppearFadingFromTop, TextSnippet.RandomDisplacement, false, 1.4f),
        new TextSnippet("me after this long???", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, false, 1f)]);

        AwesomeSentence sent2 = new(1000f, [
        new TextSnippet("downloads have flatlined bubbytron", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("there aint NO traction", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("you dont even have a server", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("nor have a", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet(" GITHUB", Color.White, .045f, TextSnippet.AppearFadingFromTop, TextSnippet.SmallRandomDisplacement, false),
        new TextSnippet("get on that game making grind", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("GRUBS AND THE BEES WILL COME TRUE!!!", Color.Red, .055f, TextSnippet.AppearFadingFromTop, TextSnippet.RandomDisplacement, true, 1.2f),
        new TextSnippet("but yeah take your time and whatever", Color.LightGray, .035f, TextSnippet.AppearFadingFromTop, null, true, .8f)]);

        AwesomeSentence sent3 = new(1000f, [
        new TextSnippet("also is asher even making the wiki? got a lot to do there...", Color.White, .035f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("...maybe??", Color.White, .075f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("at least chinny made that sick menu theme", Color.White, .035f, TextSnippet.AppearFadingFromTop, null, true, 1f),
        new TextSnippet("gotta get that", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true, 1f),
        new TextSnippet(" vinyl...", Color.Cyan, .035f, TextSnippet.AppearFadingFromTop, TextSnippet.WaveDisplacement, false, 1f),
        new TextSnippet(" ", Color.Cyan, .95f)]);

        AwesomeSentence sent4 = new(1000f, [
        new TextSnippet("ah, well, i suppose im being too harsh. Been pretty busy?", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("\n"),
        new TextSnippet("i guess theres no rush, but you cant be tmodding after highschool", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("(unless its commissions cough cough)", Color.LightGray, .045f, null, null, true, .8f),
        new TextSnippet("if your gonna make that special", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet(" game", Color.Gold, .035f, TextSnippet.AppearFadingFromTop, TextSnippet.WaveEmphasisDisplacement, false),
        new TextSnippet("you oughta be screwing around with game dev and actually learn and talk to other devs", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("amongst other things...", Color.White, .025f, TextSnippet.AppearFadingFromTop, null, true)]);
        
        AwesomeSentence sent5 = new(1000f, [
        new TextSnippet("anyways i'll take my leave", Color.White, .045f, TextSnippet.AppearFadingFromTop, null, true),
        new TextSnippet("\n"),
        new TextSnippet("goodluck!", Color.White, .045f, TextSnippet.AppearFadingFromTop, TextSnippet.SmallWaveDisplacement, true),
        new TextSnippet(" ", Color.Cyan, .55f)]);
        */
/*
        if (moddedPlayer.MouseRight.Current)
        {
            sep = 0;
            manager?.Clear();
            manager = null;
        }

        if (manager == null)
        {
            manager = new(Vector2.Zero, .9f, .2f, 0f);
            manager.AddSentence(FullDialogue);
            manager.Start();
        }

        sep++;
        manager.Update(.02f);

        device.SetRenderTarget(crtTarget);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

        if (sep == MathF.Round(FullDialogue.GetTimeToSnippet(17)))
            DirectlyDisplayText("YUA");

        // Initial background
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.DrawBetterRect(tex, ToTarget(Main.screenPosition, resolution), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, false);
        if (manager.CurrentSentence != null)
        {
            //DirectlyDisplayText($"{manager.CurrentSentence.GetCurrentSnippet(manager.CurrentProgress).index}");
            if (manager.CurrentSentence.IsSnippetActive(17, manager.CurrentProgress))
                DirectlyDisplayText("your fucking dead bucko");
        }

        // Actual text
        manager.Draw();

        Main.spriteBatch.End();
    }

    private void DrawTheTarget(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        GraphicsDevice device = Main.graphics.GraphicsDevice;
        Point resolution = new(Main.screenWidth, Main.screenHeight);
        GlobalPlayer moddedPlayer = Main.LocalPlayer.Additions();

        crtShader = AssetRegistry.GetShader("AsterlinScreen");
        crtShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
        crtShader.TrySetParameter("findingChannel", moddedPlayer.MouseRight.Current);

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, crtShader.Effect, Main.GameViewMatrix.TransformationMatrix);
        //Main.spriteBatch.Draw(crtTarget, Vector2.Zero, Color.White);
        Vector2 pos = Main.LocalPlayer.Center;

        float a = Sin01(Main.GlobalTimeWrappedHourly);
        float x = 800f;//* Animators.MakePoly(3f).InFunction(a);
        float y = 600f;//* Animators.MakePoly(12f).InFunction(a);
        Vector2 size = new Vector2(x, y);
        Main.spriteBatch.DrawBetterRect(crtTarget, ToTarget(pos, size), null, Color.White, 0f, crtTarget.Size() / 2f, SpriteEffects.None, false);

        Main.spriteBatch.End();

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Asterlin_BossChecklist);
        Main.spriteBatch.DrawBetter(tex, pos + new Vector2(0f, tex.Height * 1.5f), null, Color.White, 0f, tex.Size() / 2f, 1f);
        Main.spriteBatch.End();
    }
}
*/
public class qwerDIALOGUE : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.timeLeft = int.MaxValue;
        Projectile.Size = new(100);
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public string Key => $"Mods.{Mod.Name}.NPCs.Asterlin.";
    public static readonly Color InfoColor = Color.Goldenrod;
    public static readonly Color WarningColor = Color.Red;
    public static readonly Color StatusColor = Color.DeepSkyBlue;
    public float BoxLength = 1000f;

    public TextSnippet Pause => new(" ", Color.Transparent, .5f);
    public TextSnippet LongPause => new(" ", Color.Transparent, 3.6f);
    public AwesomeSentence Dialogue_Warnings => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatTemperatureWarning").Format(Main.time), WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatImminent").Format(Main.time), WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatHeatsinks").Format(Main.time), InfoColor, .025f, null, null, true)]);
    public AwesomeSentence Dialogue_EyeScan => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan").Format(Main.time), StatusColor),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan1").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan2").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatEyeScan3").Format(Main.time), StatusColor, .015f, null, null, true)]);
    public AwesomeSentence Dialogue_LegScan => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans").Format(Main.time), StatusColor, .02f, null, null, true),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans1").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans2").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans3").Format(Main.time), StatusColor, .015f, null, null, true)]);
    public AwesomeSentence Dialogue_CoreScan => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans").Format(Main.time), StatusColor, .02f, null, null, true),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans1").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans2").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatLegScans3").Format(Main.time), StatusColor, .015f, null, null, true)]);
    public AwesomeSentence Dialogue_ArmScan => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans").Format(Main.time), StatusColor, .02f, null, null, true),
         Pause,
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans1").Format(Main.time), StatusColor, .015f, null, null, true),
         new TextSnippet(Language.GetOrRegister($"{Key}OverheatArmScans2").Format(Main.time), StatusColor, .015f, null, null, true)]);
    public AwesomeSentence Dialogue_UhOh => new(BoxLength,
        [new TextSnippet(Language.GetOrRegister($"{Key}OverheatUhOh").Format(Main.time), InfoColor, .037f, TextSnippet.AppearFadingFromTop, TextSnippet.SmallRandomDisplacement), LongPause]);

    public AwesomeSentence[] FullDialogue => [Dialogue_Warnings, Dialogue_EyeScan, Dialogue_LegScan, Dialogue_CoreScan, Dialogue_ArmScan, Dialogue_UhOh];
    public DialogueManager manager;
    public override void AI()
    {
        if (manager == null)
        {
            manager = new DialogueManager(Projectile.Center, .9f, .18f);
            manager.AddSentences(FullDialogue);
            manager.Start();
        }
        manager?.Update(.02f);
        if (Main.LocalPlayer.Additions().MouseRight.JustPressed)
            manager?.SkipCurrentSentence();

        ParticleRegistry.SpawnDebugParticle(Projectile.Center);
        Projectile.Center = Main.LocalPlayer.Center - Vector2.UnitY * 200f - Vector2.UnitX * 300f;
        manager.Position = (Projectile.Center - Main.screenPosition);
        Projectile.velocity = Vector2.Zero;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 size = new(BoxLength, Dialogue_CoreScan.GetTotalHeight() * 2f);
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.DrawBetterRect(pixel, ToTarget(manager.Position + Main.screenPosition, size), null, Color.Black, 0f, Vector2.Zero);
        manager?.Draw();
        return false;
    }
}