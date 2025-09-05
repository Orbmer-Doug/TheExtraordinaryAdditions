using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.CrossCompatibility;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Content.Projectiles.Vanilla.SwungPickaxe;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla;

public class ToolModifier : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        if (AdditionsConfigServer.Instance.ToolOverhaul == false)
            return false;

        // (they didn't mark them correctly)
        bool extra = true;
        if (ModReferences.BaseCalamity != null)
        {
            if (ModReferences.BaseCalamity.TryFind<ModItem>("MarniteObliterator", out ModItem mar) && entity.type == mar.Type)
                extra = false;
            if (ModReferences.BaseCalamity.TryFind<ModItem>("MarniteDeconstructor", out ModItem obl) && entity.type == obl.Type)
                extra = false;
        }
        if (ModReferences.Fables != null)
        {
            if (ModReferences.Fables.TryFind<ModItem>("MarniteObliterator", out ModItem fabmar) && entity.type == fabmar.Type)
                extra = false;
            if (ModReferences.Fables.TryFind<ModItem>("MarniteDeconstructor", out ModItem fabobl) && entity.type == fabobl.Type)
                extra = false;
        }

        return lateInstantiation && (ItemID.Sets.IsDrill[entity.type] || entity.pick > 0 || entity.axe > 0 || entity.hammer > 0)
            && entity.type != ItemID.ButchersChainsaw && entity.type != ItemID.LaserDrill && entity.type != ItemID.ChlorophyteJackhammer && extra;
    }

    public override void SetDefaults(Item item)
    {
        bool channelButNoSet = item.channel && !(ItemID.Sets.IsChainsaw[item.type] || ItemID.Sets.IsDrill[item.type]);
        if (channelButNoSet)
            return;

        ItemID.Sets.SkipsInitialUseSound[item.type] = true;
        item.noMelee = true;
        item.noUseGraphic = true;

        int type = 0;
        if (ItemID.Sets.IsDrill[item.type])
            type = ModContent.ProjectileType<FancyDrill>();
        if (ItemID.Sets.IsChainsaw[item.type])
            type = ModContent.ProjectileType<FancyChainsaw>();

        if (item.channel)
            item.shoot = type;
    }

    public override void HoldItem(Item item, Player player)
    {
        // Might be unsafe to do, but it prevents the inital check from using the item
        player.toolTime = 20;
        player.controlUseItem = false;
    }

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
    {
        bool channelButNoSet = item.channel && !(ItemID.Sets.IsChainsaw[item.type] || ItemID.Sets.IsDrill[item.type]);
        if (channelButNoSet)
            return;

        int type = item.pick > 0 ? ModContent.ProjectileType<SwungPickaxe>() : item.axe > 0 ? ModContent.ProjectileType<SwungAxe>() : ModContent.ProjectileType<SwungHammer>();
        if (ItemID.Sets.IsDrill[item.type])
            type = ModContent.ProjectileType<FancyDrill>();
        if (ItemID.Sets.IsChainsaw[item.type])
            type = ModContent.ProjectileType<FancyChainsaw>();

        if (player.itemAnimation == player.itemAnimationMax && player.ownedProjectileCounts[type] == 0 && Main.myPlayer == player.whoAmI)
            Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, type, player.HeldItem.damage, player.HeldItem.knockBack, player.whoAmI);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        const string key = "ToolModifier.";
        if (item.hammer > 0)
        {
            const string name = "HammerInfo";
            string text = GetText(key + name).Value;
            tooltips.Add(new TooltipLine(Mod, name, text));
        }
        if (ItemID.Sets.IsChainsaw[item.type])
        {
            const string name = "ChainsawInfo";
            string text = GetText(key + name).Value;
            tooltips.Add(new TooltipLine(Mod, name, text));
        }
    }
}

public class SwungPickaxe : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    /// <summary>
    /// Super fine sub-division of frames
    /// </summary>
    public const int MaxUpdates = 10;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.MaxUpdates = MaxUpdates;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Init
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Vanish
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }
    public enum SwingDir
    {
        Up,
        Down,
    }
    public SwingDir State
    {
        get => (SwingDir)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = (int)value;
    }
    public ref float VanishTime => ref Projectile.Additions().ExtraAI[2];
    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }
    public ref float OverallTime => ref Projectile.Additions().ExtraAI[4];
    public ref float RotationOffset => ref Projectile.Additions().ExtraAI[5];
    public SpriteEffects SpriteDir
    {
        get => (SpriteEffects)Projectile.Additions().ExtraAI[6];
        set => Projectile.Additions().ExtraAI[6] = (int)value;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Pick => Owner.HeldItem;
    public int SwingTime => (int)(Pick.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));
    public float Scale => 1f * Owner.GetAdjustedItemScale(Pick);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Completion => InverseLerp(0f, SwingTime, Time);
    public const float Wait = .2f;
    public const float Swing = .8f;
    public const float SwingAmt = PiOver2 - .3f;

    public float GetSwing()
    {
        return SwingAmt * (new PiecewiseCurve()
            .Add(-1f, -1.2f,  Wait, MakePoly(2f).OutFunction) // Reel
            .Add(-1.2f, .9f,  Swing, MakePoly(4f).InFunction) // Swing
            .Add(.9f, 1f, 1f, MakePoly(2f).OutFunction) // End-Swing
            .Evaluate(Completion) * (State != SwingDir.Up).ToDirectionInt()) * Dir;
    }

    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height / 2);
        return new(Projectile.Center - size / 2, size, Projectile.rotation - PiOver4);
    }

    public FancyAfterimages fancy;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        fancy ??= new(7, () => Projectile.Center);

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (!Init)
        {
            Time = 0f;
            if (this.RunLocal())
                Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.ResetLocalNPCHitImmunity();
            PlayedSound = false;
            Init = true;
            this.Sync();
            return;
        }

        Projectile.width = Pick.ThisItemTexture().Width;
        Projectile.height = Pick.ThisItemTexture().Height;
        Projectile.damage = Pick.damage;
        Projectile.knockBack = Pick.knockBack;
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver4 - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Owner.itemAnimation < 2)
            Owner.itemAnimation = 2;
        if (Owner.itemTime < 2)
            Owner.itemTime = 2;

        // Fade out
        if (Vanish)
        {
            Projectile.Opacity = 1f - MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, VanishTime));
            if (Projectile.Opacity < .01f)
                Projectile.Kill();

            VanishTime++;
        }
        // Fade in
        else if (OverallTime < 10f * MaxUpdates)
        {
            Projectile.Opacity = Sine.InFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
            Projectile.scale = MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * Scale;
        }

        // Reset or vanish at the end of the swing if the player is still using the item
        if (Completion >= 1f)
        {
            if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Vanish)
            {
                Owner.SetDummyItemTime(Pick.useTime);

                if (State == SwingDir.Up)
                    State = SwingDir.Down;
                else
                    State = SwingDir.Up;
                Init = false;
                return;
            }
            else if (!Modded.SafeMouseLeft.Current && this.RunLocal())
            {
                Vanish = true;
            }
        }

        if (Completion.BetweenNum(Wait + .3f, Swing, true) && !PlayedSound)
        {
            Mine(Owner, Pick, Pick.hammer > 0);
            Pick.UseSound?.Play(Projectile.Center, 1f, 0f, .1f);
            PlayedSound = true;
        }

        bool side = false;
        if (Dir == 1 && State == SwingDir.Down)
            side = true;
        else if (Dir == -1 && State == SwingDir.Up)
            side = true;

        RotationOffset = side ? 0f : PiOver2;
        SpriteDir = side ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4 + GetSwing();

        if (Projectile.FinalExtraUpdate())
        {
            Vector2 point = Rect().RandomPoint() - Rect().Size / 2;
            Projectile.EmitEnchantmentVisualsAt(point, 1, 1);
            UpdateVisuals(Owner, Pick, Projectile.Center, point);
            ItemLoader.MeleeEffects(Pick, Owner, point.ToRectangle(1, 1));
        }
        Projectile.Center = center + PolarVector(Projectile.height, Projectile.rotation - PiOver4);

        fancy.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One,
            Projectile.Opacity, Projectile.rotation + RotationOffset, SpriteDir, 250, 0, 0f, null, false, 0f));

        Time++;
        OverallTime++;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override bool? CanCutTiles()
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }

    public override bool CanHitPvp(Player target)
    {
        return Completion.BetweenNum(Wait, 1f, true);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Pick.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor * Projectile.Opacity;
        Color glow = Color.White * Projectile.Opacity;
        float scale = Projectile.scale;
        float rot = Projectile.rotation + RotationOffset;

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Pick.Name.Contains("Solar"))
            col = glow;

        fancy.DrawFancyAfterimages(tex, [col]);
        Main.spriteBatch.Draw(tex, pos, frame, col, rot, orig, scale, SpriteDir, 0f);

        if (Pick.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Pick.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
            {
                fancy.DrawFancyAfterimages(glowmask.Value, [glow]);
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, glow, rot, orig, scale, SpriteDir, 0f);
            }
        }
        return false;
    }

    #region dumb

    /// <summary>
    /// Struck by redcode yet again
    /// </summary>
    public static void Mine(Player player, Item tool, bool hammer = false, bool right = false, Point? overrideTileTarget = null)
    {
        if (tool.pick <= 0 && tool.axe <= 0 && tool.hammer <= 0)
            return;

        bool flag = player.IsTargetTileInItemRange(tool);
        if (player.noBuilding)
            flag = false;

        if (!flag)
            return;

        Type type = typeof(Player);
        FieldInfo p = type.GetField("tileTargetX", BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
        int x = (int)p.GetValue(player);
        FieldInfo pp = type.GetField("tileTargetY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
        int y = (int)pp.GetValue(player);

        object[] miningParams = new object[4];
        miningParams[0] = tool;
        miningParams[2] = overrideTileTarget.HasValue ? overrideTileTarget.Value.X : x;
        miningParams[3] = overrideTileTarget.HasValue ? overrideTileTarget.Value.Y : y;
        MethodInfo useTool = type.GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool", BindingFlags.NonPublic | BindingFlags.Instance);
        useTool.Invoke(player, miningParams);

        if (hammer)
        {
            bool canHitWalls = (bool)miningParams[1];
            if (canHitWalls == true)
            {
                // This WOULD have gone to jackhammers (if there were more than one and was a item id set for them)
                if (right)
                {
                    for (int wx = -1; wx <= 1; wx++)
                    {
                        for (int wy = -1; wy <= 1; wy++)
                        {
                            FindWall(wx, wy);
                        }
                    }
                }
                else
                    FindWall(0, 0);

                void FindWall(int wx, int wy)
                {
                    // Try breaking walls
                    object[] wallCoords = new object[2];

                    MethodInfo findWall = type.GetMethod("ItemCheck_UseMiningTools_TryFindingWallToHammer", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
                    findWall.Invoke(player, wallCoords);

                    int wallX = (int)wallCoords[0] + wx;
                    int wallY = (int)wallCoords[1] + wy;

                    // The usual method you would use for this checks for itemAnimation and toolTime, both of which are absent in this case AND IS WHY WE CANT PUBLICIZE IT
                    MethodInfo canSmash = type.GetMethod("CanPlayerSmashWall", BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
                    bool wall = (bool)canSmash.Invoke(player, [wallX, wallY]);

                    Tile tile = Main.tile[wallX, wallY];
                    if (tile.WallType > 0 && (!tile.Active() ||
                        wallX != x || wallY != y || (!Main.tileHammer[tile.TileType] && !player.poundRelease))
                        /*&& player.controlUseItem*/ && tool.hammer > 0 && wall)
                        player.PickWall(wallX, wallY, (int)(tool.hammer * 1.5f));
                }
            }

            // Reset the hammer
            player.poundRelease = true;
        }
    }

    public static void UpdateVisuals(Player owner, Item item, Vector2 pos, Vector2 point)
    {
        // This method doesn't work?????
        /*
        owner.ItemCheck_GetMeleeHitbox(owner.HeldItem, Item.GetDrawHitbox(owner.HeldItem.type, owner), out bool dont, out Rectangle rect);
        rect = owner.ItemCheck_EmitUseVisuals(owner.HeldItem, rect);
        */

        if (item.type == ItemID.NebulaAxe || item.type == ItemID.NebulaChainsaw || item.type == ItemID.NebulaDrill || item.type == ItemID.NebulaHammer
            || item.type == ItemID.NebulaPickaxe || item.type == ItemID.LunarHamaxeNebula)
        {
            Lighting.AddLight(pos, 0.4f, 0.16f, 0.36f);
        }
        else if (item.type == ItemID.VortexAxe || item.type == ItemID.VortexChainsaw || item.type == ItemID.VortexDrill || item.type == ItemID.VortexHammer ||
            item.type == ItemID.VortexPickaxe || item.type == ItemID.LunarHamaxeVortex)
        {
            Lighting.AddLight(pos, 0f, 0.36f, 0.4f);
        }
        else if (item.type == ItemID.SolarFlareAxe || item.type == ItemID.SolarFlareChainsaw || item.type == ItemID.SolarFlareDrill || item.type == ItemID.SolarFlareHammer ||
            item.type == ItemID.SolarFlarePickaxe || item.type == ItemID.LunarHamaxeSolar)
        {
            Lighting.AddLight(pos, 0.5f, 0.25f, 0.05f);
        }
        else if (item.type == ItemID.StardustAxe || item.type == ItemID.StardustChainsaw || item.type == ItemID.StardustDrill ||
            item.type == ItemID.StardustHammer || item.type == ItemID.StardustPickaxe || item.type == ItemID.LunarHamaxeStardust)
        {
            Lighting.AddLight(pos, 0.3f, 0.3f, 0.2f);
        }

        if (item.type == ItemID.OrangePhaseblade || item.type == ItemID.OrangePhasesaber ||
            (item.type >= ItemID.BluePhaseblade && item.type <= ItemID.YellowPhaseblade) || (item.type >= ItemID.BluePhasesaber && item.type <= ItemID.YellowPhasesaber))
        {
            float r = 0.5f;
            float g = 0.5f;
            float b = 0.5f;
            if (item.type == ItemID.BluePhaseblade || item.type == ItemID.BluePhasesaber)
            {
                r *= 0.1f;
                g *= 0.5f;
                b *= 1.2f;
            }
            else if (item.type == ItemID.RedPhaseblade || item.type == ItemID.RedPhasesaber)
            {
                r *= 1f;
                g *= 0.2f;
                b *= 0.1f;
            }
            else if (item.type == ItemID.GreenPhaseblade || item.type == ItemID.GreenPhasesaber)
            {
                r *= 0.1f;
                g *= 1f;
                b *= 0.2f;
            }
            else if (item.type == ItemID.PurplePhaseblade || item.type == ItemID.PurplePhasesaber)
            {
                r *= 0.8f;
                g *= 0.1f;
                b *= 1f;
            }
            else if (item.type == ItemID.WhitePhaseblade || item.type == ItemID.WhitePhasesaber)
            {
                r *= 0.8f;
                g *= 0.9f;
                b *= 1f;
            }
            else if (item.type == ItemID.YellowPhaseblade || item.type == ItemID.YellowPhasesaber)
            {
                r *= 0.8f;
                g *= 0.8f;
                b *= 0f;
            }
            else if (item.type == ItemID.OrangePhaseblade || item.type == ItemID.OrangePhasesaber)
            {
                r *= 0.9f;
                g *= 0.5f;
                b *= 0f;
            }

            Lighting.AddLight(pos, r, g, b);
        }
    }
    #endregion
}

public class FancyDrill : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Drill => Owner.HeldItem;
    public int Wait => (int)(Drill.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));
    public float Scale => 1f * Owner.GetAdjustedItemScale(Drill);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height);
        return new(Projectile.Center - size / 2, size, Projectile.rotation);
    }

    public override void AI()
    {
        if ((!Owner.Available() || !Owner.channel) && this.RunLocal())
        {
            Projectile.Kill();
            return;
        }

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Modded.mouseWorld), .7f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = center + PolarVector(Projectile.width / 2, Projectile.rotation);
        Projectile.width = Drill.ThisItemTexture().Width;
        Projectile.height = Drill.ThisItemTexture().Height;
        Projectile.damage = Drill.damage;
        Projectile.knockBack = Drill.knockBack;
        Projectile.spriteDirection = Projectile.direction;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Projectile.soundDelay <= 0 && Time > 10)
        {
            SoundID.Item22.Play(Projectile.Center, 1f, 0f, .05f);
            SolidCollision(Rect(), 4f);
            Projectile.soundDelay = (int)Clamp(35 - Drill.useTime, 4, 100);
        }
        Projectile.Opacity = InverseLerp(0f, 10f, Time);

        // Gives the drill a slight jiggle
        Projectile.position += -Projectile.velocity * Main.rand.NextFloat(.5f, 1.5f);

        // Spawning dust
        if (Main.rand.NextBool(10) && Projectile.Opacity >= 1f)
        {
            ParticleRegistry.SpawnMistParticle(Rect().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.5f, 2f), Main.rand.NextFloat(.1f, .3f), Color.Gray, Color.DarkGray, Main.rand.NextFloat(100f, 255f), Main.rand.NextFloat(-.2f, .3f));
        }
        Time++;
    }

    public void SolidCollision(RotatedRectangle rect, float sampleIncrement = 1f, bool acceptTopSurfaces = false)
    {
        // Calculate the corners of the rotated rectangle
        Vector2 topLeft = rect.Position;
        Vector2 topRight = rect.TopRight;
        Vector2 bottomLeft = rect.BottomLeft;
        Vector2 bottomRight = rect.BottomRight;

        // Determine the number of samples to take along the width and height
        int widthSamples = (int)(rect.Width / sampleIncrement);
        int heightSamples = (int)(rect.Height / sampleIncrement);

        // Sample points in the area
        for (int i = 0; i <= widthSamples; i++)
        {
            // Lerp between the sides
            float interpolant = (float)i / widthSamples;
            Vector2 left = Vector2.Lerp(topLeft, bottomLeft, interpolant);
            Vector2 right = Vector2.Lerp(topRight, bottomRight, interpolant);

            for (int j = 0; j <= heightSamples; j++)
            {
                // Lerp inbetween the side interpolants
                Vector2 samplePoint = Vector2.Lerp(left, right, (float)j / heightSamples);

                // Convert to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                if (tile != null && tile.HasTile && !Main.tileAxe[tile.TileType])
                {
                    Mine(Owner, Drill, false, false, tilePoint);

                    if (Drill.pick <= 0 && Drill.axe <= 0 && Drill.hammer <= 0)
                        return;

                    bool flag = Owner.IsTargetTileInItemRange(Drill);
                    if (Owner.noBuilding)
                        flag = false;

                    if (!flag)
                        return;

                    for (int f = 0; f < 12; f++)
                        ParticleRegistry.SpawnSparkParticle(tilePoint.ToWorldCoordinates(), Main.rand.NextVector2Circular(9f, 9f),
                            Main.rand.Next(12, 22), Main.rand.NextFloat(.4f, .6f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat()), true, true);
                }
            }
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Drill.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor;
        float scale = Projectile.scale;
        float rotation = Projectile.rotation;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += Pi;
        }

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Drill.Name.Contains("Solar"))
            col = Color.White;
        Main.spriteBatch.Draw(tex, pos, frame, col * Projectile.Opacity, rotation, orig, scale, direction, 0f);

        if (Drill.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Drill.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, Color.White * Projectile.Opacity, rotation, orig, scale, direction, 0f);
        }

        return false;
    }
}

public class FancyChainsaw : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Chainsaw => Owner.HeldItem;
    public int Wait => (int)(Chainsaw.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));
    public float Scale => 1f * Owner.GetAdjustedItemScale(Chainsaw);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height);
        return new(Projectile.Center - size / 2, size, Projectile.rotation);
    }

    public override void AI()
    {
        if ((!Owner.Available() || !Owner.channel) && this.RunLocal())
        {
            Projectile.Kill();
            return;
        }

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Modded.mouseWorld), .7f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = center + PolarVector(Projectile.width / 2, Projectile.rotation);
        Projectile.width = Chainsaw.ThisItemTexture().Width;
        Projectile.height = Chainsaw.ThisItemTexture().Height;
        Projectile.damage = Chainsaw.damage;
        Projectile.knockBack = Chainsaw.knockBack;
        Projectile.spriteDirection = Projectile.direction;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Projectile.soundDelay <= 0 && Time > 10)
        {
            SoundID.Item22.Play(Projectile.Center, 1f, 0f, .05f);
            SolidCollision(Rect(), .8f);
            Projectile.soundDelay = (int)Clamp(30 - Chainsaw.useTime, 4, 100);
        }
        Projectile.Opacity = InverseLerp(0f, 10f, Time);

        // Gives the chainsaw a slight jiggle
        Projectile.position += -Projectile.velocity * Main.rand.NextFloat(.5f, 1.5f);

        // Spawning dust
        if (Main.rand.NextBool(10) && Projectile.Opacity >= 1f)
        {
            ParticleRegistry.SpawnMistParticle(Rect().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.5f, 2f), Main.rand.NextFloat(.1f, .3f), Color.Gray, Color.DarkGray, Main.rand.NextFloat(100f, 255f), Main.rand.NextFloat(-.2f, .3f));
        }
        Time++;
    }

    public void SolidCollision(RotatedRectangle rect, float sampleIncrement = 1f, bool acceptTopSurfaces = false)
    {
        // Calculate the corners of the rotated rectangle
        Vector2 topLeft = rect.Position;
        Vector2 topRight = rect.TopRight;
        Vector2 bottomLeft = rect.BottomLeft;
        Vector2 bottomRight = rect.BottomRight;

        // Determine the number of samples to take along the width and height
        int widthSamples = (int)(rect.Width / sampleIncrement);
        int heightSamples = (int)(rect.Height / sampleIncrement);

        // Sample points in the area
        for (int i = 0; i <= widthSamples; i++)
        {
            // Lerp between the sides
            float interpolant = (float)i / widthSamples;
            Vector2 left = Vector2.Lerp(topLeft, bottomLeft, interpolant);
            Vector2 right = Vector2.Lerp(topRight, bottomRight, interpolant);

            for (int j = 0; j <= heightSamples; j++)
            {
                // Lerp inbetween the side interpolants
                Vector2 samplePoint = Vector2.Lerp(left, right, (float)j / heightSamples);

                // Convert to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                if (tile != null && tile.HasTile)
                {
                    if (Chainsaw.pick <= 0 && Chainsaw.axe <= 0 && Chainsaw.hammer <= 0)
                        return;

                    bool flag = Owner.IsTargetTileInItemRange(Chainsaw);
                    if (Owner.noBuilding)
                        flag = false;

                    if (!flag)
                        return;

                    // drax
                    if (Chainsaw.pick > 0)
                    {
                        Mine(Owner, Chainsaw, false, false, tilePoint);

                        for (int f = 0; f < 12; f++)
                            ParticleRegistry.SpawnSparkParticle(tilePoint.ToWorldCoordinates(), Main.rand.NextVector2Circular(9f, 9f),
                                Main.rand.Next(12, 22), Main.rand.NextFloat(.4f, .6f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat()), true, true);
                    }

                    if (Main.tileAxe[tile.TileType])
                    {
                        Owner.PickTile(tilePoint.X, tilePoint.Y, Chainsaw.axe);
                    }
                    if (tile.TileType == TileID.LivingWood || tile.TileType == TileID.LeafBlock)
                    {
                        Owner.PickTile(tilePoint.X, tilePoint.Y, 200);
                    }
                }
            }
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Chainsaw.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor;
        float scale = Projectile.scale;
        float rotation = Projectile.rotation;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += Pi;
        }

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Chainsaw.Name.Contains("Solar"))
            col = Color.White;
        Main.spriteBatch.Draw(tex, pos, frame, col * Projectile.Opacity, rotation, orig, scale, direction, 0f);

        if (Chainsaw.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Chainsaw.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, Color.White * Projectile.Opacity, rotation, orig, scale, direction, 0f);
        }

        return false;
    }
}

public class SwungAxe : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    /// <summary>
    /// Super fine sub-division of frames
    /// </summary>
    public const int MaxUpdates = 10;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.MaxUpdates = MaxUpdates;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Init
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Vanish
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }
    public SwingDir State
    {
        get => (SwingDir)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = (int)value;
    }
    public ref float VanishTime => ref Projectile.Additions().ExtraAI[2];
    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }
    public ref float OverallTime => ref Projectile.Additions().ExtraAI[4];
    public ref float RotationOffset => ref Projectile.Additions().ExtraAI[5];
    public SpriteEffects SpriteDir
    {
        get => (SpriteEffects)Projectile.Additions().ExtraAI[6];
        set => Projectile.Additions().ExtraAI[6] = (int)value;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Axe => Owner.HeldItem;
    public int SwingTime => (int)(Axe.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));
    public float Scale => 1f * Owner.GetAdjustedItemScale(Axe);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Completion => InverseLerp(0f, SwingTime, Time);
    public const float Wait = .3f;
    public const float Swing = .8f;
    public const float SwingAmt = PiOver2 - .3f;

    public float GetSwing()
    {
        return SwingAmt * (new PiecewiseCurve()
            .Add(-1f, -1.2f, Wait, MakePoly(2f).OutFunction) // Reel
            .Add(-1.2f, .9f,  Swing, MakePoly(4f).InFunction) // Swing
            .Add(.9f, 1f, 1f, MakePoly(2f).OutFunction) // End-Swing
            .Evaluate(Completion) * (State != SwingDir.Up).ToDirectionInt()) * Dir;
    }

    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height / 2);
        return new(Projectile.Center - size / 2, size, Projectile.rotation - PiOver4);
    }

    public FancyAfterimages fancy;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        fancy ??= new(7, () => Projectile.Center);

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (!Init)
        {
            Time = 0f;
            if (this.RunLocal())
                Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.ResetLocalNPCHitImmunity();
            PlayedSound = false;
            Init = true;
            this.Sync();
            return;
        }

        Projectile.width = Axe.ThisItemTexture().Width;
        Projectile.height = Axe.ThisItemTexture().Height;
        Projectile.damage = Axe.damage;
        Projectile.knockBack = Axe.knockBack;
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver4 - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Owner.itemAnimation < 2)
            Owner.itemAnimation = 2;
        if (Owner.itemTime < 2)
            Owner.itemTime = 2;

        // Fade out
        if (Vanish)
        {
            Projectile.Opacity = 1f - MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, VanishTime));
            if (Projectile.Opacity < .01f)
                Projectile.Kill();

            VanishTime++;
        }
        // Fade in
        else if (OverallTime < 10f * MaxUpdates)
        {
            Projectile.Opacity = Sine.InFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
            Projectile.scale = MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * Scale;
        }

        // Reset or vanish at the end of the swing if the player is still using the item
        if (Completion >= 1f)
        {
            bool hold = Modded.SafeMouseLeft.Current || Modded.SafeMouseRight.Current;
            if (hold && !Vanish && this.RunLocal())
            {
                Owner.SetDummyItemTime(Axe.useTime);

                if (State == SwingDir.Up)
                    State = SwingDir.Down;
                else
                    State = SwingDir.Up;
                Init = false;
                return;
            }
            else if (!hold && this.RunLocal())
            {
                Vanish = true;
            }
        }

        if (Completion.BetweenNum(Wait + .3f, Swing, true) && !PlayedSound)
        {
            Mine(Owner, Axe, Axe.hammer > 0, Modded.SafeMouseRight.Current);
            Axe.UseSound?.Play(Projectile.Center, 1f, 0f, .1f);
            PlayedSound = true;
        }

        bool side = false;
        if (Dir == 1 && State == SwingDir.Down)
            side = true;
        else if (Dir == -1 && State == SwingDir.Up)
            side = true;

        RotationOffset = side ? 0f : PiOver2;
        SpriteDir = side ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4 + GetSwing();

        if (Projectile.FinalExtraUpdate())
        {
            Vector2 point = Rect().RandomPoint() - Rect().Size / 2;
            Projectile.EmitEnchantmentVisualsAt(point, 1, 1);
            UpdateVisuals(Owner, Axe, Projectile.Center, point);
            ItemLoader.MeleeEffects(Axe, Owner, point.ToRectangle(1, 1));

            // an attempt at making shoot work, but it appears that doing such makes 1000 projectiles a frame depending on how that item is coded
            //ItemLoader.Shoot(Axe, Owner, new(Owner, Axe, Axe.shoot), Projectile.Center, Projectile.velocity, Axe.shoot, Axe.damage, Axe.knockBack);
        }
        Projectile.Center = center + PolarVector(Projectile.height, Projectile.rotation - PiOver4);

        fancy.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One,
            Projectile.Opacity, Projectile.rotation + RotationOffset, SpriteDir, 250, 0, 0f, null, false, 0f));

        Time++;
        OverallTime++;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override bool? CanCutTiles()
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }

    public override bool CanHitPvp(Player target)
    {
        return Completion.BetweenNum(Wait, 1f, true);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ItemLoader.OnHitNPC(Axe, Owner, target, hit, damageDone);
        NPCLoader.OnHitByItem(target, Owner, Axe, hit, damageDone);
        PlayerLoader.OnHitNPC(Owner, target, hit, damageDone);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        ItemLoader.OnHitPvp(Axe, Owner, target, info);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Axe.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor * Projectile.Opacity;
        Color glow = Color.White * Projectile.Opacity;
        float scale = Projectile.scale;
        float rot = Projectile.rotation + RotationOffset;

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Axe.Name.Contains("Solar"))
            col = glow;

        fancy.DrawFancyAfterimages(tex, [col]);
        Main.spriteBatch.Draw(tex, pos, frame, col, rot, orig, scale, SpriteDir, 0f);

        if (Axe.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Axe.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
            {
                fancy.DrawFancyAfterimages(glowmask.Value, [glow]);
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, glow, rot, orig, scale, SpriteDir, 0f);
            }
        }
        return false;
    }
}

public class SwungHammer : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    /// <summary>
    /// Super fine sub-division of frames
    /// </summary>
    public const int MaxUpdates = 10;
    public override void SetStaticDefaults()
    {
        // Prevent jittering when the player walks up slopes and such
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.MaxUpdates = MaxUpdates;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Init
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Vanish
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }
    public SwingDir State
    {
        get => (SwingDir)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = (int)value;
    }
    public ref float VanishTime => ref Projectile.Additions().ExtraAI[2];
    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }
    public ref float OverallTime => ref Projectile.Additions().ExtraAI[4];
    public ref float RotationOffset => ref Projectile.Additions().ExtraAI[5];
    public SpriteEffects SpriteDir
    {
        get => (SpriteEffects)Projectile.Additions().ExtraAI[6];
        set => Projectile.Additions().ExtraAI[6] = (int)value;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Hammer => Owner.HeldItem;
    public int SwingTime => (int)(Hammer.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));
    public float Scale => 1f * Owner.GetAdjustedItemScale(Hammer);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Completion => InverseLerp(0f, SwingTime, Time);
    public const float Wait = .2f;
    public const float Swing = .8f;
    public const float SwingAmt = PiOver2 - .3f;

    public float GetSwing()
    {
        return SwingAmt * (new PiecewiseCurve()
            .Add(-1f, -1.2f,  Wait, MakePoly(2f).OutFunction) // Reel
            .Add(-1.2f, .9f,  Swing, MakePoly(4f).InFunction) // Swing
            .Add(.9f, 1f, 1f, MakePoly(2f).OutFunction) // End-Swing
            .Evaluate(Completion) * (State != SwingDir.Up).ToDirectionInt()) * Dir;
    }

    public RotatedRectangle Rect()
    {
        Vector2 size = new(Projectile.width, Projectile.height / 2);
        return new(Projectile.Center - size / 2, size, Projectile.rotation - PiOver4);
    }

    public FancyAfterimages fancy;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        fancy ??= new(7, () => Projectile.Center);

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (!Init)
        {
            Time = 0f;
            Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.ResetLocalNPCHitImmunity();
            PlayedSound = false;
            Init = true;
            this.Sync();
            return;
        }

        Projectile.width = Hammer.ThisItemTexture().Width;
        Projectile.height = Hammer.ThisItemTexture().Height;
        Projectile.damage = Hammer.damage;
        Projectile.knockBack = Hammer.knockBack;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemRotation = (Projectile.velocity * Dir).ToRotation();
        Owner.ChangeDir(Dir);
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver4 - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Owner.itemAnimation < 2)
            Owner.itemAnimation = 2;
        if (Owner.itemTime < 2)
            Owner.itemTime = 2;

        // Fade out
        if (Vanish)
        {
            Projectile.Opacity = 1f - MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, VanishTime));
            if (Projectile.Opacity < .01f)
                Projectile.Kill();

            VanishTime++;
        }
        // Fade in
        else if (OverallTime < 10f * MaxUpdates)
        {
            Projectile.Opacity = Sine.InFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
            Projectile.scale = MakePoly(3).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * Scale;
        }

        // Reset or vanish at the end of the swing if the player is still using the item
        if (Completion >= 1f)
        {
            if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Vanish)
            {
                Owner.SetDummyItemTime(Hammer.useTime);

                if (State == SwingDir.Up)
                    State = SwingDir.Down;
                else
                    State = SwingDir.Up;
                Init = false;
                return;
            }
            else if (!Modded.SafeMouseLeft.Current && this.RunLocal())
            {
                Vanish = true;
            }
        }

        if (Completion.BetweenNum(Wait + .3f, Swing, true) && !PlayedSound)
        {
            Mine(Owner, Hammer, true, Modded.SafeMouseRight.Current);
            Hammer.UseSound?.Play(Projectile.Center, 1f, 0f, .1f);
            PlayedSound = true;
        }

        bool side = false;
        if (Dir == 1 && State == SwingDir.Down)
            side = true;
        else if (Dir == -1 && State == SwingDir.Up)
            side = true;

        RotationOffset = side ? 0f : PiOver2;
        SpriteDir = side ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4 + GetSwing();

        if (Projectile.FinalExtraUpdate())
        {
            Vector2 point = Rect().RandomPoint() - Rect().Size / 2;
            Projectile.EmitEnchantmentVisualsAt(point, 1, 1);
            UpdateVisuals(Owner, Hammer, Projectile.Center, point);
            ItemLoader.MeleeEffects(Hammer, Owner, point.ToRectangle(1, 1));
        }
        Projectile.Center = center + PolarVector(Projectile.height, Projectile.rotation - PiOver4);

        fancy.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One,
            Projectile.Opacity, Projectile.rotation + RotationOffset, SpriteDir, 250, 0, 0f, null, false, 0f));

        Time++;
        OverallTime++;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override bool? CanCutTiles()
    {
        return Completion.BetweenNum(Wait, 1f, true) ? null : false;
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().Bottom, Rect().Top, Rect().Width, DelegateMethods.CutTiles);
    }

    public override bool CanHitPvp(Player target)
    {
        return Completion.BetweenNum(Wait, 1f, true);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        RotatedRectangle rect = Rect();
        Vector2 start = rect.Bottom;
        Vector2 end = rect.Top;
        float width = rect.Width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Hammer.ThisItemTexture();
        Vector2 orig = tex.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor * Projectile.Opacity;
        Color glow = Color.White * Projectile.Opacity;
        float scale = Projectile.scale;
        float rot = Projectile.rotation + RotationOffset;

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        if (Hammer.Name.Contains("Solar"))
            col = glow;

        fancy.DrawFancyAfterimages(tex, [col]);
        Main.spriteBatch.Draw(tex, pos, frame, col, rot, orig, scale, SpriteDir, 0f);

        if (Hammer.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Hammer.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
            {
                fancy.DrawFancyAfterimages(glowmask.Value, [glow]);
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, glow, rot, orig, scale, SpriteDir, 0f);
            }
        }
        return false;
    }
}