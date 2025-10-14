using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

/// Do a silly animation upon killing all players
public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_GetScrewed()
    {
        StateMachine.RegisterStateBehavior(AsterlinAIType.GetScrewed, DoBehavior_GetScrewed);
    }

    private enum AnimType
    {
        Waving,
        idk,
        Judging,
        AbsoluteChinema,
        Party,
        GetJiggy
    }

    private static readonly WeightedDict<AnimType> animDict = new(new()
    {
        { AnimType.Waving, .5f },
        { AnimType.idk, .9f },
        { AnimType.Judging, .7f },
        { AnimType.AbsoluteChinema, .7f },
        { AnimType.Party, .7f },
        { AnimType.GetJiggy, .5f },
    });

    private AnimType GetScrewed_Type
    {
        get => (AnimType)ExtraAI[0];
        set => ExtraAI[0] = (int)value;
    }

    public static readonly float GetScrewed_MaxTime = SecondsToFrames(2.5f);
    public void DoBehavior_GetScrewed()
    {
        if (AITimer == 1 && this.RunServer())
        {
            GetScrewed_Type = animDict.GetRandom();
            NPC.netUpdate = true;
        }

        switch (GetScrewed_Type)
        {
            case AnimType.Waving:
                SetLookingStraight(true);
                float time = AITimer * .1f;
                float dirleft = -Utils.Remap(BezierEase(Cos01(time)), 0, 1, ThreePIOver4, PiOver4);
                SetLeftHandTarget(LeftArm.RootPosition + PolarVector(400f, dirleft));
                break;

            case AnimType.idk:
                float progress = (float)AITimer / GetScrewed_MaxTime;
                PiecewiseCurve danceCurve = new PiecewiseCurve()
                            .Add(0f, 1f, 0.5f, Sine.InOutFunction)
                            .AddStall(1f, 1f)
                            .Add(1f, 0f, 0.5f, Sine.InOutFunction);
                float danceT = danceCurve.Evaluate(progress);
                SetBodyRotation(Lerp(-0.3f, 0.3f, Sine.InOutFunction(danceT)));
                SetZPosition(Lerp(0.7f, 1f, Bounce.OutFunction(danceT)));
                SetLeftHandTarget(NPC.Center + PolarVector(200f * ZPosition, BodyRotation + PiOver2 + Lerp(-0.5f * Direction, 0.5f * Direction, Sine.OutFunction(danceT))));
                SetRightHandTarget(NPC.Center + PolarVector(200f * ZPosition, BodyRotation - PiOver2 + Lerp(0.5f * Direction, -0.5f * Direction, Sine.OutFunction(danceT))));
                float legSwing = Lerp(-0.4f * Direction, 0.4f * Direction, Convert01To010(Sin01(danceT * 3)));
                SetLeftLegRotation(legSwing);
                SetRightLegRotation(-legSwing);
                break;

            case AnimType.Judging:
                SetZPosition(1f - MakePoly(2f).InFunction(InverseLerp(0f, GetScrewed_MaxTime, AITimer)));
                SetLookingStraight(true);
                break;

            case AnimType.AbsoluteChinema:
                SetLookingStraight(true);
                SetLeftHandTarget(LeftArm.RootPosition + PolarVector(400f, -PiOver2));
                SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, -PiOver2));
                SetEyeGleam(NPC.Opacity);
                break;

            case AnimType.Party:
                SetDirection(-1);
                SetHeadRotation(Pi);
                float partyTime = AITimer * .25f;
                float swingAmt = .9f;
                SetLeftHandTarget(LeftArm.RootPosition + PolarVector(400f, -Utils.Remap(Cos01(partyTime), 0, 1, -swingAmt - Pi, swingAmt - Pi)));
                SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, Utils.Remap(Cos01(partyTime), 0, 1, -swingAmt - Pi, swingAmt - Pi)));
                SetZPosition(NPC.Opacity);
                break;

            case AnimType.GetJiggy:
                SetLookingStraight(true);
                float jiggyTime = AITimer * .25f;
                float max = .7f;
                SetLeftHandTarget(LeftArm.RootPosition + PolarVector(400f, -Utils.Remap(Cos01(jiggyTime), 0, 1, -max - PiOver2, max - PiOver2)));
                SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, -Utils.Remap(Cos01(jiggyTime), 0, 1, -max - PiOver2, max - PiOver2)));
                SetLeftLegRotation(Utils.Remap(Cos01(jiggyTime), 0, 1, -.3f, .3f));
                SetRightLegRotation(Utils.Remap(Cos01(jiggyTime), 0, 1, -.3f, .3f));
                SetBodyRotation(Utils.Remap(Cos01(jiggyTime), 0, 1, -.15f, .15f));
                SetEyeGleam(NPC.Opacity);
                break;
        }

        NPC.Opacity = InverseLerp(GetScrewed_MaxTime, GetScrewed_MaxTime - 40, AITimer);
        SetLegFlamesInterpolant(NPC.Opacity);
        if (AITimer >= GetScrewed_MaxTime)
        {
            NPC.active = false;
            NPC.netUpdate = true;
            ProjOwnedByNPC<Asterlin>.KillAll();
        }
    }
}