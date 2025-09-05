using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

// Lucille... i remember your PARADIGMS
[AutoloadBossHead]
public partial class Asterlin : ModNPC
{
    #region Enums
    public enum AsterlinAIType : int
    {
        // pondering the orb
        AbsorbingEnergy,

        // Phase 1
        Swings,
        RotatedDicing,
        Barrage,

        // Phase 2
        Cleave,
        Disintegration,
        Lightripper,

        // Phase 3
        Judgement,
        Hyperbeam,
        TechnicBombBarrage,

        // Desperation
        UnveilingZenith,
        UnrelentingRush,
        QuickHyperbeam,

        // Phase Transitions
        EnterPhase2,
        EnterPhase3,
        DesperationDrama,
        GabrielLeave,

        // Intermediate states
        GetScrewed,
        ResetCycle,
    }

    #endregion

    #region Balancing
    /// <summary>
    /// Represents the 1 - 0 ratio of life for asterlin
    /// </summary>
    public float LifeRatio => InverseLerp(0f, NPC.lifeMax, NPC.life);
    public const float Phase2LifeRatio = 0.65f;
    public const float Phase3LifeRatio = 0.3f;

    public static int LightAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int MediumAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int HeavyAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int SuperHeavyAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    #endregion

    private static NPC myself;
    public static NPC Myself
    {
        get
        {
            if (myself is not null && !myself.active)
                return null;

            return myself;
        }
        internal set => myself = value;
    }

    private CyberneticSword Sword;
    private TheTechnicBlitzripper Gun;
    private TheTesselesticMeltdown Staff;

    /// <summary>
    /// The aimed target for Asterlin, whether it be a player or NPC
    /// </summary>
    public NPCAimedTarget Target;
    public Player PlayerTarget;

    /// <summary>
    /// The state that Asterlin is currently in
    /// </summary>
    public AsterlinAIType CurrentState
    {
        get
        {
            // Add the relevant phase cycle if it has been exhausted
            if (StateMachine.StateStack is not null && (StateMachine?.StateStack?.Count ?? 1) <= 0)
                StateMachine?.StateStack.Push(StateMachine.StateRegistry[AsterlinAIType.ResetCycle]);

            return StateMachine?.CurrentState?.Identifier ?? AsterlinAIType.AbsorbingEnergy;
        }
    }

    public bool FightStarted
    {
        get => NPC.ai[0] == 1f;
        set => NPC.ai[0] = value.ToInt();
    }

    /*
     * Note: ExtraAI 0 - 10 are reserved for attack states and will be reset per transition
     */

    public bool DonePhase2Transition
    {
        get => NPC.AdditionsInfo().ExtraAI[11] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[11] = value.ToInt();
    }
    public bool DonePhase3Transition
    {
        get => NPC.AdditionsInfo().ExtraAI[12] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[12] = value.ToInt();
    }
    public bool DoneDesperationTransition
    {
        get => NPC.AdditionsInfo().ExtraAI[13] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[13] = value.ToInt();
    }
    public ref float HeatDistortionArea => ref NPC.AdditionsInfo().ExtraAI[14];
    public ref float HeatDistortionStrength => ref NPC.AdditionsInfo().ExtraAI[15];

    /// <summary>
    /// this very stupid counter is to apply velocity changes to the target because for some ungodly reason you cant do that in the on hit method
    /// </summary>
    public ref float TargetHitEffectCounter => ref NPC.AdditionsInfo().ExtraAI[16];

    #region AI
    public override void AI()
    {
        // Pick a target if the current one is invalid
        bool invalidTargetIndex = Target.Invalid;
        if (invalidTargetIndex || !NPC.WithinRange(Target.Center, 4600f))
            PlayerTargeting.SearchForTarget(NPC, Target);
        Target = NPC.GetTargetData();
        if (NPC.HasValidTarget && Target.Type == Terraria.Enums.NPCTargetType.Player)
            PlayerTarget = Main.player[NPC.target];

        // Set the global NPC instance
        Myself = NPC;
        if (Myself == null)
            return;

        // Reset variables every frame
        NPC.damage = 0;
        NPC.defense = NPC.defDefense;
        NPC.dontTakeDamage = false;
        NPC.immortal = false;
        NPC.ShowNameOnHover = true;

        if (FightStarted)
        {
            NPC.noTileCollide = true;
            NPC.noGravity = true;

            // Disable lifesteal for all players and give them infinite flight
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.ghost)
                    continue;

                player.GrantInfiniteFlight();
                player.GrantBossEffectsBuff();

                player.moonLeech = true;
            }
        }

        NPC.scale = ZPosition;
        if (NPC.scale < .6f)
            NPC.ShowNameOnHover = false;
        int oldWidth = NPC.width;
        int idealWidth = (int)(NPC.scale * 128f);
        int idealHeight = (int)(NPC.scale * 278f);
        if (idealWidth != oldWidth)
        {
            NPC.position.X += NPC.width / 2;
            NPC.position.Y += NPC.height / 2;
            NPC.width = idealWidth;
            NPC.height = idealHeight;
            NPC.position.X -= NPC.width / 2;
            NPC.position.Y -= NPC.height / 2;
        }

        // Disable damage when invisible
        if (NPC.Opacity <= 0.35f)
        {
            NPC.ShowNameOnHover = false;
            NPC.dontTakeDamage = true;
        }

        // you dare despawn
        NPC.timeLeft = 7200;

        if (TargetHitEffectCounter > 0f)
        {
            Player p = Main.player[NPC.target];
            p.Additions().LungingDown = true;
            p.velocity = NPC.SafeDirectionTo(p.Center) * 30f;
            TargetHitEffectCounter--;
        }
        if (TargetHitEffectCounter <= 0f)
        {
            Player p = Main.player[NPC.target];
            p.Additions().LungingDown = false;
        }

        ResetGraphics();
        SearchForArsenalWeapons();
        try
        {
            StateMachine?.PerformBehaviors();
            StateMachine?.PerformStateTransitionCheck();
        }
        catch (Exception ex)
        {
            throw new Exception($"Uh oh! Asterlin just went kaboom because: {ex.Message} \n State: {CurrentState}, State Identity: {StateMachine.CurrentState.Identifier}, Timer: {AITimer}");
        }
        AITimer++;
        UpdateGraphics();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        return null;
    }

    public override bool CheckDead()
    {
        if (CurrentState == AsterlinAIType.GabrielLeave)
            return true;

        // Keep life at 1
        NPC.life = 1;
        NPC.netUpdate = true;
        return false;
    }

    public override void OnKill()
    {
        ModContent.GetInstance<DownedBossSystem>().AsterlinDowned = true;
        AdditionsNetcode.SyncWorld();
    }
    #endregion AI
}



public class qwerPA : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
    }

    public enum DebugPattern
    {
        Idle,
        Attack1,
        Attack2,
        Attack3,

        Transition,
        Attack4,
        Attack5,
        Attack6,
        Leave
    }

    private RandomPushdownAutomata<EntityAIState<DebugPattern>, DebugPattern> pushdownAutomata;
    public RandomPushdownAutomata<EntityAIState<DebugPattern>, DebugPattern> PushdownAutomata
    {
        get
        {
            if (pushdownAutomata is null)
                LoadStates();
            return pushdownAutomata!;
        }
        set => pushdownAutomata = value;
    }
    public void LoadStates()
    {
        // Initialize the AI state machine.
        PushdownAutomata = new(new(DebugPattern.Idle));
        PushdownAutomata.OnStateTransition += ResetVariables;

        // Register all of Asterlins states in the machine.
        foreach (DebugPattern type in Enum.GetValues(typeof(DebugPattern)))
            PushdownAutomata.RegisterState(new EntityAIState<DebugPattern>(type));

        PushdownAutomata.ApplyToAllStatesExcept(state =>
        {
            PushdownAutomata.RegisterTransition(state, new Dictionary<DebugPattern, float> { { DebugPattern.Transition, 1f } }, false, () => Main.LocalPlayer.Additions().MouseLeft.JustPressed, () =>
            {

            });
        }, DebugPattern.Transition);

        // Load state transitions.
        AutomatedMethodInvokeAttribute.InvokeWithAttribute(this);
    }
    public void ResetVariables(bool stateWasPopped, EntityAIState<DebugPattern> oldState)
    {
        PushdownAutomata.CurrentState.Time = 0;
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Idle()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Idle, new Dictionary<DebugPattern, float> { { DebugPattern.Attack1, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 90;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Idle, DoBehavior_Idle);
    }
    public void DoBehavior_Idle()
    {
        DirectlyDisplayText("haha i do nothing");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack1()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack1, new Dictionary<DebugPattern, float> { { DebugPattern.Attack2, 1f }, { DebugPattern.Attack3, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack1, DoBehavior_Attack1);
    }
    public void DoBehavior_Attack1()
    {

        DirectlyDisplayText("in attack1");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack2()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack2, new Dictionary<DebugPattern, float> { { DebugPattern.Attack1, 1f }, { DebugPattern.Attack3, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack2, DoBehavior_Attack2);
    }
    public void DoBehavior_Attack2()
    {

        DirectlyDisplayText("in attack2");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack3()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack3, new Dictionary<DebugPattern, float> { { DebugPattern.Attack2, 1f }, { DebugPattern.Attack3, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack3, DoBehavior_Attack3);
    }
    public void DoBehavior_Attack3()
    {
        DirectlyDisplayText("in attack3");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Transition()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Transition, new Dictionary<DebugPattern, float> { { DebugPattern.Attack4, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 120;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Transition, DoBehavior_Transition);
    }
    public void DoBehavior_Transition()
    {
        DirectlyDisplayText("transitioning it rn");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack4()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack4, new Dictionary<DebugPattern, float> { { DebugPattern.Attack5, 1f }, { DebugPattern.Attack6, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack4, DoBehavior_Attack4);
    }
    public void DoBehavior_Attack4()
    {
        DirectlyDisplayText("in attack4");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack5()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack5, new Dictionary<DebugPattern, float> { { DebugPattern.Attack4, 1f }, { DebugPattern.Attack6, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack5, DoBehavior_Attack5);
    }
    public void DoBehavior_Attack5()
    {
        DirectlyDisplayText("in attack5");
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Attack6()
    {
        PushdownAutomata.RegisterTransition(DebugPattern.Attack6, new Dictionary<DebugPattern, float> { { DebugPattern.Attack4, 1f }, { DebugPattern.Attack5, 1f } }, false, () =>
        {
            return PushdownAutomata.CurrentState.Time >= 60;
        });

        // Load the AI state behavior.
        PushdownAutomata.RegisterStateBehavior(DebugPattern.Attack6, DoBehavior_Attack6);
    }
    public void DoBehavior_Attack6()
    {
        DirectlyDisplayText("in attack6");
    }

    public override void AI()
    {
        // debug things
        ParticleRegistry.SpawnDebugParticle(Projectile.Center);
        Projectile.Center = Main.MouseWorld;
        Projectile.timeLeft = 2;
        if (Main.LocalPlayer.Additions().MouseMiddle.Current)
            Projectile.Kill();

        DirectlyDisplayText($"{PushdownAutomata.CurrentState.Time}");

        // Run the PA
        PushdownAutomata.CurrentState.Time++; // Increment time each frame
        PushdownAutomata.PerformStateTransitionCheck();
        PushdownAutomata.PerformBehaviors();
    }
}