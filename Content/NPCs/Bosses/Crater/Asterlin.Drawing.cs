using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC, IHasScreenShader
{
    public static AsterlinTarget target;

    // i dont actually want to make a atlas system but i dont want 19 images in the asset folder
    public static readonly Rectangle HeadRect = new(0, 0, 46, 60);
    public static readonly Rectangle BodyRect = new(46, 0, 104, 156);

    public static readonly Rectangle LeftAngledBackLimbRect = new(150, 0, 24, 18);
    public static readonly Rectangle LeftAngledForeLimbRect = new(174, 0, 26, 28);
    public static readonly Rectangle LeftAngledHandRect = new(200, 0, 26, 28);

    public static readonly Rectangle RightAngledBackLimbRect = new(150, 30, 28, 28);
    public static readonly Rectangle RightAngledForeLimbRect = new(178, 30, 22, 30);
    public static readonly Rectangle RightAngledHandRect = new(200, 30, 24, 26);

    public static readonly Rectangle LeftAngledLegRect = new(226, 0, 50, 90);
    public static readonly Rectangle RightAngledLegRect = new(276, 0, 46, 90);

    public static readonly Rectangle StraightBodyRect = new(0, 156, 110, 192);

    public static readonly Rectangle LeftStraightBackLimbRect = new(110, 156, 28, 30);
    public static readonly Rectangle LeftStraightForeLimbRect = new(138, 156, 20, 30);
    public static readonly Rectangle LeftStraightHandRect = new(158, 156, 16, 28);

    public static readonly Rectangle RightStraightBackLimbRect = new(110, 186, 28, 30);
    public static readonly Rectangle RightStraightForeLimbRect = new(138, 186, 20, 32);
    public static readonly Rectangle RightStraightHandRect = new(158, 186, 16, 26);

    public static readonly Rectangle LeftStraightLegRect = new(174, 156, 40, 92);
    public static readonly Rectangle RightStraightLegRect = new(214, 156, 40, 92);

    public RotatedRectangle RotatedHitbox;
    public Vector2 EyePosition;
    public Vector2 LeftHandPosition;
    public Vector2 RightHandPosition;
    public Vector2 LeftFootPosition;
    public Vector2 RightFootPosition;
    public Vector2 LeftVentPosition;
    public Vector2 RightVentPosition;

    public Vector2 LeftHandTarget;
    public Vector2 RightHandTarget;

    public float BodyRotation;
    public float LeftLegRotation;
    public float RightLegRotation;
    public float HeadRotation;

    private float _ZPosition;
    /// <summary>
    /// Controls how "close" Asterlin should be to the screen to give psuedo 3D <br></br>
    /// Takes an input between 0 - 1 and outputs a value between .5 - 1 <br></br>
    /// .5 for far away, 1 for close up
    /// </summary>
    public float ZPosition
    {
        get => _ZPosition;
        set => _ZPosition = Utils.Remap(value, 0f, 1f, .5f, 1f);
    }

    public bool LookingStraight;

    public int Direction;

    public bool Flipped;

    public float LegFlamesInterpolant;

    public float FlameEngulfInterpolant;

    // These variables do not reset after being assigned
    public float EyeGleamInterpolant;

    public float GlowInterpolant;

    public float VentGlowInterpolant;

    public float PowerInterpolant;

    // Create a lot of backing bools for aforementioned fields to control
    // whether or not they should resume to their idle state or should be manipulated in AI
    // Unsure if a centralized state-based preset manager would be better, but it
    // would practically be the same thing but with extra steps
    // Either way it gets quite hairy
    private bool ManualLeftHandTarget;
    private bool ManualRightHandTarget;
    private bool ManualBodyRotation;
    private bool ManualLeftLegRotation;
    private bool ManualRightLegRotation;
    private bool ManualHeadRotation;
    private bool ManualZPosition;
    private bool ManualEyeGleam;
    private bool ManualLookingStraight;
    private bool ManualDirection;
    private bool ManualFlipped;
    private bool ManualLegFlamesInterpolant;

    public void SetLeftHandTarget(Vector2 target)
    {
        LeftHandTarget = target;
        ManualLeftHandTarget = true;
    }
    public void SetRightHandTarget(Vector2 target)
    {
        RightHandTarget = target;
        ManualRightHandTarget = true;
    }
    public void SetBodyRotation(float target)
    {
        BodyRotation = target;
        ManualBodyRotation = true;
    }
    public void SetLeftLegRotation(float target)
    {
        LeftLegRotation = target;
        ManualLeftLegRotation = true;
    }
    public void SetRightLegRotation(float target)
    {
        RightLegRotation = target;
        ManualRightLegRotation = true;
    }
    public void SetHeadRotation(float target)
    {
        HeadRotation = target;
        ManualHeadRotation = true;
    }
    public void SetZPosition(float target)
    {
        ZPosition = target;
        ManualZPosition = true;
    }
    public void SetEyeGleam(float target)
    {
        EyeGleamInterpolant = target;
        ManualEyeGleam = true;
    }
    public void SetLookingStraight(bool target)
    {
        LookingStraight = target;
        ManualLookingStraight = true;
    }
    public void SetDirection(int target)
    {
        Direction = target;
        ManualDirection = true;
    }
    public void SetFlipped(bool target)
    {
        Flipped = target;
        ManualFlipped = true;
    }
    public void SetLegFlamesInterpolant(float target)
    {
        LegFlamesInterpolant = target;
        ManualLegFlamesInterpolant = true;
    }

    public JointChain leftArm;
    public JointChain rightArm;

    public void ResetGraphics()
    {
        ManualLeftHandTarget = ManualRightHandTarget = ManualBodyRotation = ManualLeftLegRotation = ManualRightLegRotation =
            ManualHeadRotation = ManualZPosition = ManualEyeGleam = ManualLookingStraight = ManualDirection = ManualFlipped = ManualLegFlamesInterpolant = LookingStraight = false;
    }

    public void UpdateGraphics()
    {
        if (!LookingStraight && !ManualDirection)
            Direction = (Target.Center.X < NPC.Center.X).ToDirectionInt();
        else
            Direction = 1;

        if (!ManualBodyRotation)
            BodyRotation = MathHelper.Clamp(BodyRotation.AngleLerp(NPC.velocity.X * .02f, .1f), -.4f, .4f);

        if (!ManualFlipped)
        {
            Flipped = false;

            if (Direction == 1 && float.IsNegative(MathF.Cos(BodyRotation)))
            {
                BodyRotation += Pi;
                Flipped = true;
            }
        }
        else
        {
            if (Flipped)
                BodyRotation += Pi;
        }

        if (!ManualZPosition)
            ZPosition = MathHelper.Lerp(ZPosition, 1f, .1f);

        if (!ManualEyeGleam)
            EyeGleamInterpolant = MathHelper.Lerp(EyeGleamInterpolant, 0f, .1f);

        if (!ManualLegFlamesInterpolant)
            LegFlamesInterpolant = MathHelper.Lerp(LegFlamesInterpolant, 1f, .2f);

        List<CCDKinematicJoint> leftJoints = leftArm.Root.GetSubLimb();
        List<CCDKinematicJoint> rightJoints = rightArm.Root.GetSubLimb();

        // 0 is the root
        // 1 is the back limb
        // 2 is the fore limb
        // 3 is the hand
        if (!LookingStraight) // Angled
        {
            leftJoints[0].JointLength = LeftAngledBackLimbRect.Height * ZPosition;
            rightJoints[0].JointLength = RightAngledBackLimbRect.Height * ZPosition;
            leftJoints[1].JointLength = LeftAngledBackLimbRect.Height * ZPosition;
            rightJoints[1].JointLength = RightAngledBackLimbRect.Height * ZPosition;
            leftJoints[2].JointLength = LeftAngledForeLimbRect.Height * ZPosition;
            rightJoints[2].JointLength = RightAngledForeLimbRect.Height * ZPosition;
            leftJoints[3].JointLength = LeftAngledHandRect.Height * ZPosition;
            rightJoints[3].JointLength = RightAngledHandRect.Height * ZPosition;
        }
        else
        {
            leftJoints[0].JointLength = LeftStraightBackLimbRect.Height * ZPosition;
            rightJoints[0].JointLength = RightStraightBackLimbRect.Height * ZPosition;
            leftJoints[1].JointLength = LeftStraightBackLimbRect.Height * ZPosition;
            rightJoints[1].JointLength = RightStraightBackLimbRect.Height * ZPosition;
            leftJoints[2].JointLength = LeftStraightForeLimbRect.Height * ZPosition;
            rightJoints[2].JointLength = RightStraightForeLimbRect.Height * ZPosition;
            leftJoints[3].JointLength = LeftStraightHandRect.Height * ZPosition;
            rightJoints[3].JointLength = RightStraightHandRect.Height * ZPosition;
        }

        Vector2 leftRoot = LookingStraight ? NPC.Center + PolarVector(13f * ZPosition, BodyRotation - PiOver2) + PolarVector(44f * ZPosition * Direction, BodyRotation + Pi) :
            NPC.Center + PolarVector(26f * ZPosition, BodyRotation - PiOver2) + PolarVector(42f * Direction * ZPosition, BodyRotation + Pi);
        leftArm.RootPosition = leftRoot + NPC.velocity;
        float leftHandRot = leftJoints[2].Position.AngleTo(leftJoints[3].Position) - PiOver2;
        Vector2 leftHandOffset = LookingStraight ? PolarVector(4f * ZPosition * Direction, leftHandRot + Pi) :
            PolarVector(11f * ZPosition * Direction, leftHandRot + Pi) + PolarVector(8f * ZPosition, leftHandRot - PiOver2);
        leftArm.Update(LeftHandTarget - leftHandOffset, 50);
        if (!ManualLeftHandTarget)
            LeftHandTarget = Vector2.Lerp(LeftHandTarget, leftArm.RootPosition + PolarVector(200f, BodyRotation + MathHelper.PiOver2), .1f);

        Vector2 rightRoot = LookingStraight ? NPC.Center + PolarVector(13f * ZPosition, BodyRotation - PiOver2) + PolarVector(46f * ZPosition * Direction, BodyRotation) :
            NPC.Center + PolarVector(33f * ZPosition, BodyRotation - PiOver2) + PolarVector(42f * Direction * ZPosition, BodyRotation);
        rightArm.RootPosition = rightRoot + NPC.velocity;
        float rightHandRot = rightJoints[2].Position.AngleTo(rightJoints[3].Position) - PiOver2;
        Vector2 rightHandOffset = PolarVector(6f * Direction, rightHandRot);
        rightArm.Update(RightHandTarget - rightHandOffset, 50);
        if (!ManualRightHandTarget)
            RightHandTarget = Vector2.Lerp(RightHandTarget, leftArm.RootPosition + PolarVector(200f, BodyRotation + MathHelper.PiOver2), .1f);

        LeftHandPosition = leftJoints[2].Position + leftHandOffset + PolarVector(LeftAngledHandRect.Height, leftHandRot + PiOver2);
        RightHandPosition = rightJoints[2].Position + rightHandOffset + PolarVector(RightAngledHandRect.Height, rightHandRot + PiOver2);

        LeftVentPosition = LookingStraight ? NPC.Center + PolarVector(18f * ZPosition * Direction, BodyRotation + Pi) + PolarVector(5f * ZPosition, BodyRotation - PiOver2) :
            NPC.Center + PolarVector(28f * ZPosition * Direction, BodyRotation + Pi) + PolarVector(26f * ZPosition, BodyRotation - PiOver2);
        RightVentPosition = LookingStraight ? NPC.Center + PolarVector(18f * ZPosition * Direction, BodyRotation) + PolarVector(5f * ZPosition, BodyRotation - PiOver2) :
            NPC.Center + PolarVector(26f * ZPosition, BodyRotation - PiOver2);

        EyePosition = LookingStraight ? NPC.Center + PolarVector(52f, BodyRotation - PiOver2) :
            NPC.Center + PolarVector(62f * ZPosition, BodyRotation - PiOver2) + PolarVector(2f * ZPosition * Direction, BodyRotation) + PolarVector(18f * ZPosition * -Direction, (-.91f * -Direction) + HeadRotation);

        if (!ManualHeadRotation)
            HeadRotation = EyePosition.AngleTo(Target.Center);
        if (Direction == 1 && !LookingStraight)
            HeadRotation += Pi;

        float velRot = Vector2.Dot(NPC.velocity, BodyRotation.ToRotationVector2()) * .03f;
        if (!ManualLeftLegRotation)
            LeftLegRotation = LeftLegRotation.AngleLerp(velRot, .2f);
        if (!ManualRightLegRotation)
            RightLegRotation = RightLegRotation.AngleLerp(velRot, .2f);

        float leftLegAdd = (LookingStraight ? LeftStraightLegRect.Height : LeftAngledLegRect.Height);
        Vector2 leftShift = PolarVector(leftLegAdd * ZPosition, (BodyRotation + LeftLegRotation) + PiOver2);
        Vector2 leftLegPos = LookingStraight ? NPC.Center + PolarVector(90f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation + Pi) :
            NPC.Center + PolarVector(68f * ZPosition, BodyRotation + PiOver2) + PolarVector(27f * ZPosition * Direction, BodyRotation + Pi);

        LeftFootPosition = leftLegPos + leftShift + NPC.velocity;

        float rightLegAdd = (LookingStraight ? RightStraightLegRect.Height : RightAngledLegRect.Height);
        Vector2 rightShift = PolarVector(rightLegAdd * ZPosition, (BodyRotation + RightLegRotation) + PiOver2);
        Vector2 rightLegPos = LookingStraight ? NPC.Center + PolarVector(90f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation) :
            NPC.Center + PolarVector(68f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation);

        RightFootPosition = rightLegPos + rightShift + NPC.velocity;

        if (LeftLegFlame == null || LeftLegFlame._disposed)
            LeftLegFlame = new(FlameWidthFunct, FlameColorFunct, null, 30);
        if (RightLegFlame == null || RightLegFlame._disposed)
            RightLegFlame = new(FlameWidthFunct, FlameColorFunct, null, 30);

        OldPositions.Update(NPC.Center);

        float dist = MathHelper.Lerp(110f, 200f, AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f) * LegFlamesInterpolant;
        for (int i = 0; i < LeftLegPoints.Points.Length; i++)
        {
            LeftLegPoints.SetPoint(i, Vector2.Lerp(OldPositions.Points[i] + (LeftFootPosition - NPC.Center), LeftFootPosition, .5f)
                - PolarVector(1f, ((BodyRotation + LeftLegRotation) - MathHelper.PiOver2)) * i / (float)(LeftLegPoints.Points.Length - 1f) * dist);
            RightLegPoints.SetPoint(i, Vector2.Lerp(OldPositions.Points[i] + (RightFootPosition - NPC.Center), RightFootPosition, .5f)
                - PolarVector(1f, ((BodyRotation + RightLegRotation) - MathHelper.PiOver2)) * i / (float)(RightLegPoints.Points.Length - 1f) * dist);
        }

        if (FlameEngulfTrail == null || FlameEngulfTrail._disposed)
            FlameEngulfTrail = new(FlameEngulfWidthFunct, FlameEngulfColorFunct, null, 8);

        RotatedHitbox = new(NPC.position, NPC.Size, BodyRotation);
        FlameEngulfPoints.Update(RotatedHitbox.Center + NPC.velocity + NPC.velocity.SafeNormalize(Vector2.Zero) * RotatedHitbox.Height / 2);
    }

    public void DrawGlowForPiece(Texture2D glow, Vector2 position, Rectangle source, float rotation, Vector2 origin, SpriteEffects flip)
    {
        const int glowCount = 10;
        for (int x = 0; x < glowCount; x++)
        {
            Vector2 offset = PolarVector(MathHelper.Lerp(0f, 2f, GlowInterpolant), MathHelper.TwoPi * InverseLerp(0f, glowCount, x) + (Main.GlobalTimeWrappedHourly * .5f));
            Color color = Color.DeepSkyBlue with { A = 0 } * GlowInterpolant;
            Main.spriteBatch.DrawBetter(glow, position + offset, source, color, rotation, origin, ZPosition, flip);
        }
    }

    public void DrawLeftArm(Texture2D atlas, Texture2D glow, Color color, SpriteEffects flip)
    {
        if (leftArm != null)
        {
            // Collect all joints in the chain
            List<CCDKinematicJoint> joints = leftArm.Root.GetSubLimb();

            float backLimbRot = joints[0].Position.AngleTo(joints[1].Position) - PiOver2;
            Rectangle backLimbSource = LookingStraight ? LeftStraightBackLimbRect : LeftAngledBackLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[0].Position,
                backLimbSource, color, backLimbRot, new(backLimbSource.Width / 2f, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[0].Position, backLimbSource, backLimbRot, new(backLimbSource.Width / 2f, 0f), flip);

            float foreLimbRot = joints[1].Position.AngleTo(joints[2].Position) - PiOver2;
            Rectangle foreLimbSource = LookingStraight ? LeftStraightForeLimbRect : LeftAngledForeLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[1].Position - PolarVector(4f * Direction, foreLimbRot),
                foreLimbSource, color, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[1].Position - PolarVector(4f * Direction, foreLimbRot), foreLimbSource, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), flip);

            float handRot = joints[2].Position.AngleTo(joints[3].Position) - PiOver2;
            Rectangle handSource = LookingStraight ? LeftStraightHandRect : LeftAngledHandRect;
            Main.spriteBatch.DrawBetter(atlas, joints[2].Position + (LookingStraight ? PolarVector(4f * ZPosition * Direction, handRot + Pi) : PolarVector(11f * ZPosition * Direction, handRot + Pi) + PolarVector(8f * ZPosition, handRot - PiOver2)),
                handSource, color, handRot, new(handSource.Width / 2, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[2].Position + (LookingStraight ? PolarVector(4f * ZPosition * Direction, handRot + Pi) : PolarVector(11f * ZPosition * Direction, handRot + Pi) + PolarVector(8f * ZPosition, handRot - PiOver2)), handSource, handRot, new(handSource.Width / 2f, 0f), flip);
        }
    }

    public void DrawRightArm(Texture2D atlas, Texture2D glow, Color color, SpriteEffects flip)
    {
        if (rightArm != null)
        {
            List<CCDKinematicJoint> joints = rightArm.Root.GetSubLimb();

            float backLimbRot = joints[0].Position.AngleTo(joints[1].Position) - PiOver2;
            Rectangle backLimbSource = LookingStraight ? RightStraightBackLimbRect : RightAngledBackLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[0].Position,
                backLimbSource, color, backLimbRot, new(backLimbSource.Width / 2f, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[0].Position, backLimbSource, backLimbRot, new(backLimbSource.Width / 2f, 0f), flip);

            float foreLimbRot = joints[1].Position.AngleTo(joints[2].Position) - PiOver2;
            Rectangle foreLimbSource = LookingStraight ? RightStraightForeLimbRect : RightAngledForeLimbRect;
            Main.spriteBatch.DrawBetter(atlas, joints[1].Position + PolarVector(5f * Direction, foreLimbRot),
                foreLimbSource, color, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[1].Position + PolarVector(5f * Direction, foreLimbRot), foreLimbSource, foreLimbRot, new(foreLimbSource.Width / 2f, 0f), flip);

            float handRot = joints[2].Position.AngleTo(joints[3].Position) - PiOver2;
            Rectangle handSource = LookingStraight ? RightStraightHandRect : RightAngledHandRect;
            Main.spriteBatch.DrawBetter(atlas, joints[2].Position + PolarVector(6f * Direction, handRot),
                handSource, color, handRot, new(handSource.Width / 2, 0f), ZPosition, flip);
            DrawGlowForPiece(glow, joints[2].Position + PolarVector(6f * Direction, handRot), handSource, handRot, new(handSource.Width / 2f, 0f), flip);
        }
    }

    public void DrawBody(Texture2D atlas, Texture2D glow, Texture2D vent, Color color, SpriteEffects flip)
    {
        Rectangle bodySource = LookingStraight ? StraightBodyRect : BodyRect;
        Main.spriteBatch.DrawBetter(atlas, NPC.Center, bodySource, color, BodyRotation, bodySource.Size() / 2f, ZPosition, flip);
        DrawGlowForPiece(glow, NPC.Center, bodySource, BodyRotation, bodySource.Size() / 2f, flip);

        const int glowCount = 10;
        for (int x = 0; x < glowCount; x++)
        {
            Vector2 offset = PolarVector(MathHelper.Lerp(0f, 6f, VentGlowInterpolant), MathHelper.TwoPi * InverseLerp(0f, glowCount, x));
            Color ventColor = Color.OrangeRed with { A = 0 } * VentGlowInterpolant;
            Main.spriteBatch.DrawBetter(vent, NPC.Center + offset, bodySource, ventColor, BodyRotation, bodySource.Size() / 2f, ZPosition, flip);
        }
    }

    public void DrawHead(Texture2D atlas, Texture2D glow, Color color, SpriteEffects flip)
    {
        if (!LookingStraight)
        {
            Vector2 headPos = NPC.Center + PolarVector(60f * ZPosition, BodyRotation - PiOver2) + PolarVector(2f * ZPosition * Direction, BodyRotation);
            Vector2 headOrig = new Vector2(HeadRect.Width / 2, HeadRect.Height);
            Main.spriteBatch.DrawBetter(atlas, headPos, HeadRect, color, HeadRotation, headOrig, ZPosition, flip);
            DrawGlowForPiece(glow, headPos, HeadRect, HeadRotation, headOrig, flip);
        }

        if (EyeGleamInterpolant > 0f)
        {
            void gleam()
            {
                Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Vector2 scale = new(Animators.MakePoly(4f).OutFunction.Evaluate(0f, 400f, EyeGleamInterpolant), Animators.MakePoly(2f).InOutFunction.Evaluate(0f, 50f, EyeGleamInterpolant));
                float rot = BodyRotation;
                Main.spriteBatch.DrawBetterRect(star, ToTarget(EyePosition + NPC.velocity, scale * .4f), null, Color.LightCyan, rot, star.Size() / 2);
                Main.spriteBatch.DrawBetterRect(star, ToTarget(EyePosition + NPC.velocity, scale), null, Color.Cyan, rot, star.Size() / 2);
            }
            PixelationSystem.QueueTextureRenderAction(gleam, PixelationLayer.OverNPCs, BlendState.Additive);
        }
    }

    public TrailPoints OldPositions = new(30);

    public ManualTrailPoints LeftLegPoints = new(30);
    public OptimizedPrimitiveTrail LeftLegFlame;

    public ManualTrailPoints RightLegPoints = new(30);
    public OptimizedPrimitiveTrail RightLegFlame;

    public float FlameWidthFunct(float completionRatio) => MathHelper.SmoothStep(32f, 8f, Animators.MakePoly(1.4f).OutFunction(completionRatio)) * NPC.scale;

    public Color FlameColorFunct(SystemVector2 completionRatio, Vector2 pos)
    {
        Color startingColor = Color.Lerp(Color.LightSkyBlue, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.DarkBlue, Color.Cyan, 0.2f);
        Color endColor = Color.Lerp(Color.DarkCyan, Color.Cyan, 0.67f);
        return MulticolorLerp(completionRatio.X, startingColor, middleColor, endColor) * GetLerpBump(0f, .1f, .8f, .27f, completionRatio.X) * NPC.Opacity * LegFlamesInterpolant;
    }

    public void DrawLegs(Texture2D atlas, Texture2D glow, Color color, SpriteEffects flip)
    {
        Vector2 leftLegPos = LookingStraight ? NPC.Center + PolarVector(90f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation + Pi) :
            NPC.Center + PolarVector(68f * ZPosition, BodyRotation + PiOver2) + PolarVector(27f * ZPosition * Direction, BodyRotation + Pi);
        Vector2 rightLegPos = LookingStraight ? NPC.Center + PolarVector(90f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation) :
            NPC.Center + PolarVector(68f * ZPosition, BodyRotation + PiOver2) + PolarVector(21f * ZPosition * Direction, BodyRotation);
        Rectangle leftLegSource = LookingStraight ? LeftStraightLegRect : LeftAngledLegRect;
        Rectangle rightLegSource = LookingStraight ? RightStraightLegRect : RightAngledLegRect;
        Main.spriteBatch.DrawBetter(atlas, leftLegPos,
            leftLegSource, color, LeftLegRotation + BodyRotation, new Vector2(leftLegSource.Width / 2, 0f), ZPosition, flip);
        DrawGlowForPiece(glow, leftLegPos, leftLegSource, LeftLegRotation + BodyRotation, new Vector2(leftLegSource.Width / 2, 0f), flip);

        Main.spriteBatch.DrawBetter(atlas, rightLegPos,
            rightLegSource, color, RightLegRotation + BodyRotation, new Vector2(rightLegSource.Width / 2, 0f), ZPosition, flip);
        DrawGlowForPiece(glow, rightLegPos, rightLegSource, RightLegRotation + BodyRotation, new Vector2(rightLegSource.Width / 2, 0f), flip);

        void flame()
        {
            ManagedShader shader = ShaderRegistry.SmoothFlame;
            shader.TrySetParameter("heatInterpolant", 1.9f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);

            if (LeftLegFlame != null && LeftLegPoints != null)
            {
                LeftLegFlame.DrawTrail(shader, LeftLegPoints.Points, 200, true, true);
            }

            if (RightLegFlame != null && RightLegPoints != null)
            {
                RightLegFlame.DrawTrail(shader, RightLegPoints.Points, 200, true, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(flame, PixelationLayer.OverNPCs, null);
    }

    public TrailPoints FlameEngulfPoints = new(8);
    public OptimizedPrimitiveTrail FlameEngulfTrail;
    public float FlameEngulfWidthFunct(float c)
    {
        float baseWidth = MathHelper.Lerp(RotatedHitbox.Height, 50f, c);
        float tipSmoothenFactor = MathF.Sqrt(1f - Animators.MakePoly(3f).InFunction(InverseLerp(0.3f, 0.015f, c)));
        return NPC.scale * baseWidth * tipSmoothenFactor;
    }

    public Color FlameEngulfColorFunct(SystemVector2 c, Vector2 pos)
    {
        Color trailColor = MulticolorLerp(Animators.MakePoly(2.45f).OutFunction(c.X) * 0.7f, new(196, 240, 255), new(125, 222, 255), new(31, 198, 255));
        return trailColor * (1 - c.X) * FlameEngulfInterpolant;
    }

    public void DrawFlameEngulf()
    {
        void engulf()
        {
            if (FlameEngulfTrail != null && FlameEngulfTrail._disposed == false && FlameEngulfPoints != null)
            {
                ManagedShader shader = AssetRegistry.GetShader("FlameEngulfShader");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1, SamplerState.AnisotropicWrap);
                shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 1.2f);
                FlameEngulfTrail.DrawTrail(shader, FlameEngulfPoints.Points, 100, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(engulf, PixelationLayer.OverNPCs);
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("HeatDistortionFilter");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Shader.TrySetParameter("intensity", HeatDistortionStrength);
        Shader.TrySetParameter("screenPos", NPC.Center - Main.screenPosition);
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("radius", HeatDistortionArea / Main.screenWidth);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("HeatDistortionFilter", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }
    public bool IsEntityActive() => NPC.active;

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (HeatDistortionStrength != 0f)
        {
            if (!HasShader)
                InitializeShader();
            UpdateShader();
        }

        /*
        Texture2D atlas = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlas);
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlasGlow);
        Texture2D vent = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlasVentGlow);

        SpriteEffects flip = LookingStraight ? SpriteEffects.None : (Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        Color color = Color.White.Lerp(Color.Gray, 1f - ZPosition) * NPC.Opacity;

        if (!LookingStraight)
        {
            DrawLeftArm(atlas, glow, color, flip);
            if (CurrentState == AsterlinAIType.TechnicBombBarrage)
                TechnicBombBarrage_Draw();
        }

        DrawBody(atlas, glow, vent, color, flip);
        DrawLegs(atlas, glow, color, flip);
        DrawHead(atlas, glow, color, flip);
        if (CurrentState == AsterlinAIType.TechnicBombBarrage)
            Gun?.DrawGun();

        if (LookingStraight)
        {
            DrawLeftArm(atlas, glow, color, flip);
            DrawRightArm(atlas, glow, color, flip);
        }
        else
        {
            DrawRightArm(atlas, glow, color, flip);
        }
        if (CurrentState == AsterlinAIType.RotatedDicing)
            RotatedDicing_Draw();
        if (CurrentState == AsterlinAIType.TechnicBombBarrage)
            TechnicBombBarrage_DrawReticle();
        if (CurrentState == AsterlinAIType.UnrelentingRush)
            UnrelentingRush_DrawTelegraph();
        if (CurrentState == AsterlinAIType.AbsorbingEnergy)
            AbsorbingEnergy_Draw();
        DrawFlameEngulf();
        */
        RenderFromTarget();

        return false;
    }

    public static void LoadTarget()
    {
        target = new();
        Main.ContentThatNeedsRenderTargets.Add(target);
    }

    public static void RenderToTarget()
    {
        if (!FindNPC(out NPC npc, ModContent.NPCType<Asterlin>()))
            return;
        Asterlin asterlin = npc.As<Asterlin>();

        Texture2D atlas = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlas);
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlasGlow);
        Texture2D vent = AssetRegistry.GetTexture(AdditionsTexture.AsterlinAtlasVentGlow);

        SpriteEffects flip = asterlin.LookingStraight ? SpriteEffects.None : (asterlin.Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        Color color = Color.White.Lerp(Color.Gray, 1f - asterlin.ZPosition) * asterlin.NPC.Opacity;

        if (!asterlin.LookingStraight)
        {
            asterlin.DrawLeftArm(atlas, glow, color, flip);
            if (asterlin.CurrentState == AsterlinAIType.TechnicBombBarrage)
                asterlin.TechnicBombBarrage_Draw();
        }

        asterlin.DrawBody(atlas, glow, vent, color, flip);
        asterlin.DrawLegs(atlas, glow, color, flip);
        asterlin.DrawHead(atlas, glow, color, flip);
        if (asterlin.CurrentState == AsterlinAIType.TechnicBombBarrage)
            asterlin.Gun?.DrawGun();

        if (asterlin.LookingStraight)
        {
            asterlin.DrawLeftArm(atlas, glow, color, flip);
            asterlin.DrawRightArm(atlas, glow, color, flip);
        }
        else
        {
            asterlin.DrawRightArm(atlas, glow, color, flip);
        }
        if (asterlin.CurrentState == AsterlinAIType.RotatedDicing)
            asterlin.RotatedDicing_Draw();
        if (asterlin.CurrentState == AsterlinAIType.TechnicBombBarrage)
            asterlin.TechnicBombBarrage_DrawReticle();
        if (asterlin.CurrentState == AsterlinAIType.UnrelentingRush)
            asterlin.UnrelentingRush_DrawTelegraph();
        if (asterlin.CurrentState == AsterlinAIType.AbsorbingEnergy)
            asterlin.AbsorbingEnergy_Draw();
        asterlin.DrawFlameEngulf();
    }

    public static void RenderFromTarget()
    {
        target.Request();

        // Wait until the drawers ready
        if (!target.IsReady)
            return;

        if (!FindNPC(out NPC npc, ModContent.NPCType<Asterlin>()))
            return;
        Asterlin asterlin = npc.As<Asterlin>();

        if (asterlin.PowerInterpolant > 0f)
        {
            ManagedShader shader = AssetRegistry.GetShader("AsterlinPower");
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 3f);
            shader.TrySetParameter("resolution", new Vector2(2000f));
            shader.TrySetParameter("opacity", asterlin.PowerInterpolant);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, shader.Effect, Main.GameViewMatrix.TransformationMatrix);
        }
        Main.spriteBatch.Draw(target.GetTarget(), Main.screenLastPosition - Main.screenPosition, Color.White);
        if (asterlin.PowerInterpolant > 0f)
            Main.spriteBatch.ResetToDefault();
    }
}

public class AsterlinTarget : ARenderTargetContentByRequest
{
    public override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        Vector2 size = new(device.Viewport.Width, device.Viewport.Height);
        PrepareARenderTarget_WithoutListeningToEvents(ref _target, Main.instance.GraphicsDevice, (int)size.X, (int)size.Y, RenderTargetUsage.PreserveContents);

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        Asterlin.RenderToTarget();
        Main.spriteBatch.End();

        device.SetRenderTarget(null);

        _wasPrepared = true;
    }
}