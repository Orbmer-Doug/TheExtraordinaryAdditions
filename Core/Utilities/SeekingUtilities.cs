using CalamityMod;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Utilities;

/// <summary>
/// Find a specific entity
/// </summary>
public static partial class Utility
{
    /// <summary>
    /// Calculates the velocity needed for a chaser to intercept a target, accounting for the target's velocity
    /// </summary>
    /// <returns>The velocity vector for the chaser to intercept the target, or towards the target's predicted position if interception is impossible within constraints</returns>
    public static Vector2 GetHomingVelocity(Vector2 chaserPos, Vector2 targetPos, Vector2 targetVel, float chaserSpeed, float maxPredictionFrames = float.MaxValue)
    {
        if (chaserSpeed <= 0f)
            return Vector2.Zero;

        // Relative position and velocity
        Vector2 relativePos = targetPos - chaserPos;
        float distanceSquared = relativePos.LengthSquared();

        if (distanceSquared < 1f) // Chaser is effectively at the target
            return Vector2.Zero;

        // Quadratic coefficients: at^2 + bt + c = 0
        float a = targetVel.LengthSquared() - chaserSpeed * chaserSpeed;
        float b = 2f * Vector2.Dot(relativePos, targetVel);
        float c = distanceSquared;

        float? timeToIntercept = SolveQuadratic(a, b, c);

        if (timeToIntercept.HasValue && timeToIntercept.Value > 0f)
        {
            float t = timeToIntercept.Value;
            if (t <= maxPredictionFrames)
            {
                // Exact interception possible within time limit
                Vector2 interceptPoint = targetPos + targetVel * t;
                Vector2 direction = (interceptPoint - chaserPos).SafeNormalize(Vector2.Zero);
                return direction * chaserSpeed;
            }
        }

        // If no interception within maxPredictionFrames or impossible, move towards predicted position
        float predictionTime = Math.Min(maxPredictionFrames, relativePos.Length() / chaserSpeed);
        Vector2 predictedPos = targetPos + targetVel * predictionTime;
        Vector2 fallbackDirection = (predictedPos - chaserPos).SafeNormalize(Vector2.Zero);
        return fallbackDirection * chaserSpeed;
    }

    /// <summary>
    /// A simple way to check if this target is a active hostile NPC
    /// <br></br>
    /// Does not account for regular seeking data
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool TargetValid(this NPC target)
    {
        if (target is null || !target.active || target.friendly || !target.CanBeChasedBy())
            return false;
        return true;
    }

    /// <param name="proj">The projectile in question</param>
    /// <param name="dist">Distance to check for other projectiles</param>
    /// <param name="limit">Makes a limit for how many to grab (should probably be kept to lower numbers)</param>
    /// <param name="sort">Should the list sort itself by distance?</param>
    /// <returns>Any other projectile of the same type that is not this current projectile</returns>
    public static List<Projectile> GetOtherProjs(this Projectile proj, float dist, int limit = 50, bool sort = false)
    {
        List<Projectile> otherProjs = [];
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile other = Main.projectile[i];
            if (other == null || other.active == false || otherProjs.Count >= limit ||
                other.whoAmI == proj.whoAmI || other.type != proj.type || !proj.WithinRange(other.Center, dist))
                continue;

            otherProjs.Add(other);
        }

        if (sort)
        {
            List<Projectile> possible = otherProjs.DistinctBy(n => n.Distance(proj.Center)).ToList();
            possible.Sort((a, b) => Vector2.Distance(a.Center, proj.Center) > Vector2.Distance(b.Center, proj.Center) ? 1 : -1);
        }

        return otherProjs;
    }

    public static void KillAllHostileProjectiles()
    {
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj.active && proj.hostile && !proj.friendly && proj.damage > 0)
                proj.Kill();
        }
    }

    public static void KillShootProjectiles(bool shouldBreak, int projType, Player player)
    {
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj.active && proj.owner == player.whoAmI && proj.type == projType)
            {
                proj.Kill();
                if (shouldBreak)
                    break;
            }
        }
    }

    public static void DeleteAllProjectiles(bool setToInactive, params int[] projectileIDs)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (!Main.projectile[i].active || !projectileIDs.Contains(Main.projectile[i].type))
                continue;

            Projectile p = Main.projectile[i];
            if (setToInactive)
            {
                p.active = false;
                p.netUpdate = true;
            }
            else
                p.Kill();
        }
    }

    public static void DeleteAllOwnerProjectiles(this Player owner, bool setToInactive, params int[] projectileIDs)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (!Main.projectile[i].active || !projectileIDs.Contains(Main.projectile[i].type))
                continue;

            Projectile p = Main.projectile[i];
            if (p.owner != owner.whoAmI)
                continue;

            if (setToInactive)
            {
                p.active = false;
                p.netUpdate = true;
            }
            else
                p.Kill();
        }
    }

    public static int CountNPCs(params int[] typesToCheck)
    {
        // Don't waste time if the type check list is empty for some reason.
        if (typesToCheck.Length <= 0)
            return 0;

        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
        {
            if (!typesToCheck.Contains(n.type))
                continue;

            count++;
        }

        return count;
    }

    public static bool AnyProjectile(int type)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p != null && p.active && p.type == type)
                return true;
        }

        return false;
    }

    public static Span<Projectile> AllProjectilesByID(int type)
    {
        int count = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active)
                count++;
        }

        Projectile[] projs = new Projectile[count];

        int index = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active)
            {
                projs[index] = projectile;
                index++;
            }
        }

        return projs.AsSpan();
    }

    public static Span<Projectile> AllProjectilesFromOwner(int type, Player player)
    {
        int count = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active && projectile.owner == player.whoAmI)
                count++;
        }

        Projectile[] projs = new Projectile[count];

        int index = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active && projectile.owner == player.whoAmI)
            {
                projs[index] = projectile;
                index++;
            }
        }

        return projs.AsSpan();
    }

    public static int CountProjectiles(int type)
    {
        int count = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active && projectile.type == type)
                count++;
        }
        return count;
    }

    public static int CountOwnerProjectiles(this Player player, int type)
    {
        int count = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.active && projectile.type == type && projectile.owner == player.whoAmI)
                count++;
        }
        return count;
    }

    public static void OnlyOneSentry(Player player, int type)
    {
        int existingTurrets = player.ownedProjectileCounts[type];
        if (existingTurrets <= 0)
            return;

        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.type == type && projectile.owner == player.whoAmI && projectile.active)
            {
                projectile.Kill();
                existingTurrets--;
                if (existingTurrets <= 0)
                {
                    break;
                }
            }
        }
    }

    public static void PlayerCount(out int total, out int alive)
    {
        total = 0;
        alive = 0;
        foreach (Player player in Main.ActivePlayers)
        {
            total++;
            if (player.dead || player.ghost)
                continue;
            alive++;
        }
    }

    public static bool IsOffscreen(this Projectile p)
    {
        // Check whether the projectile's hitbox intersects the screen, accounting for the screen fluff setting.
        int fluff = ProjectileID.Sets.DrawScreenCheckFluff[p.type];
        Rectangle screenArea = new((int)Main.Camera.ScaledPosition.X - fluff, (int)Main.Camera.ScaledPosition.Y - fluff, (int)Main.Camera.ScaledSize.X + fluff * 2, (int)Main.Camera.ScaledSize.Y + fluff * 2);
        return !screenArea.Intersects(p.Hitbox);
    }

    public static bool IsAnEnemy(this NPC npc, bool allowStatues = true)
    {
        if (npc == null || !npc.active || npc.townNPC || npc.friendly)
            return false;
        if (!allowStatues && npc.SpawnedFromStatue)
            return false;
        if (npc.lifeMax <= 5 || npc.damage <= 5 && npc.lifeMax <= 3000)
            return false;
        if (npc.type == NPCID.TargetDummy || npc.lifeMax > 25000000)
            return false;

        return true;
    }

    public static bool IsEater(this NPC target) => target.type >= NPCID.EaterofWorldsHead && target.type <= NPCID.EaterofWorldsTail;

    public static bool IsDestroyer(this NPC target) => target.type >= NPCID.TheDestroyer && target.type <= NPCID.TheDestroyerTail;

    public static bool IsThanatos(this NPC target) => target.type == ModContent.NPCType<ThanatosBody1>() ||
        target.type == ModContent.NPCType<ThanatosBody2>() ||
        target.type == ModContent.NPCType<ThanatosTail>() ||
        target.type == ModContent.NPCType<ThanatosHead>();

    public static bool IsAquaticScoog(this NPC target) => target.type == ModContent.NPCType<AquaticScourgeBody>() ||
        target.type == ModContent.NPCType<AquaticScourgeBodyAlt>() ||
        target.type == ModContent.NPCType<AquaticScourgeTail>() ||
        target.type == ModContent.NPCType<AquaticScourgeHead>();

    public static bool IsDevourer(this NPC target) => target.type == ModContent.NPCType<DevourerofGodsBody>() ||
        target.type == ModContent.NPCType<DevourerofGodsTail>() ||
        target.type == ModContent.NPCType<DevourerofGodsHead>();

    public static bool IsWormBoss(this NPC target) => target.IsThanatos() || target.IsDevourer() || target.IsAquaticScoog() || target.IsDestroyer() || target.IsEater();

    public static bool AnyBosses()
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc is null || npc.active == false)
                continue;
            if (npc.boss || npc.IsEater())
                return true;
        }

        return false;
    }

    public static bool FindItem(out Item item, Player player, int wantedType)
    {
        if (player.HeldItem.type == wantedType)
        {
            item = player.HeldItem;
            return true;
        }

        item = null;
        return false;
    }

    public static bool FindProjectile(out Projectile projectile, int wantedType, int? owner = null)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p != null && p.active && p.type == wantedType)
            {
                if (owner != null)
                {
                    if (p.owner != owner.Value)
                        continue;
                }

                projectile = p;
                return true;
            }
        }

        projectile = null;
        return false;
    }

    public static bool FindNPC(out NPC npc, int wantedType)
    {
        foreach (NPC n in Main.ActiveNPCs)
        {
            if (n != null && n.active && n.type == wantedType)
            {
                npc = n;
                return true;
            }
        }

        npc = null;
        return false;
    }
}

#region Targeting
public static class NPCTargeting
{
    /// <param name="Origin">Where to start at</param>
    /// <param name="Radius">Maximum radius to account for</param>
    /// <param name="LOS">Check for line of sight?</param>
    /// <param name="BossPriority">Whether to priortize NPC's classified as bosses</param>
    /// <param name="ExemptTargets">Blacklist certain npcs</param>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct NPCSeekingData(Vector2 Origin, int Radius, bool LOS = false, bool BossPriority = false, List<NPC> ExemptTargets = null)
    {
        public override readonly string ToString()
        {
            return $"Origin={Origin}, Radius={Radius}, Line of Sight? {LOS}, BossPriority? {BossPriority}, Exemptions={(ExemptTargets != null ? string.Join(", ", ExemptTargets) : "null")}";
        }
    }

    private static bool IsValidTarget(NPC npc, Vector2 origin, float radiusSquared, NPCSeekingData data)
    {
        return npc.active &&
               !npc.friendly &&
               !npc.dontTakeDamage &&
               !npc.immortal &&
               !npc.townNPC &&
               npc.chaseable &&
               npc.lifeMax > 5 &&
               Vector2.DistanceSquared(origin, npc.Center) <= radiusSquared &&
               (data.ExemptTargets == null || !data.ExemptTargets.Contains(npc)) &&
               (!data.LOS || Collision.CanHit(origin, 1, 1, npc.Center, 1, 1));
    }

    public static bool CanHomeInto(this NPC npc, bool ignoreInvis = false, bool ignoreInvincibility = false)
    {
        if (npc != null && npc.active)
            return npc.life > 0 && npc.CanBeChasedBy(null, ignoreInvincibility) && !npc.townNPC && npc.Opacity > 0f;
        return false;
    }

    public static NPC MinionHoming(NPCSeekingData data, Player owner, bool checksRange = false)
    {
        // Validate owner and target index
        if (owner == null || !owner.whoAmI.WithinBounds(byte.MaxValue) || !owner.MinionAttackTargetNPC.WithinBounds(Main.maxNPCs))
        {
            return GetClosestNPC(data);
        }

        // Check owner's minion attack target
        NPC targetNPC = Main.npc[owner.MinionAttackTargetNPC];
        if (owner.HasMinionAttackTargetNPC && targetNPC.active)
        {
            bool canHit = !data.LOS || Collision.CanHit(data.Origin, 1, 1, targetNPC.Center, 1, 1);
            float extraDistance = (targetNPC.width + targetNPC.height) / 2f; // Average of width/height
            bool distCheck = !checksRange || Vector2.Distance(data.Origin, targetNPC.Center) < data.Radius + extraDistance;

            if (canHit && distCheck)
                return targetNPC;
        }

        // Fallback to closest NPC
        return GetClosestNPC(data);
    }

    public static List<NPC> GetNPCsClosestToFarthest(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<NPC> bosses = [];
        List<NPC> nonBosses = [];

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
                bosses.Add(npc);
            else
                nonBosses.Add(npc);
        }

        List<NPC> validNPCs = data.BossPriority && bosses.Count > 0 ? bosses : nonBosses;
        validNPCs.Sort((a, b) =>
        {
            float distA = Vector2.DistanceSquared(origin, a.Center);
            float distB = Vector2.DistanceSquared(origin, b.Center);
            int distCompare = distA.CompareTo(distB);
            return distCompare != 0 ? distCompare : a.whoAmI.CompareTo(b.whoAmI);
        });

        return validNPCs;
    }

    public static NPC GetClosestNPC(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        NPC closestBoss = null;
        NPC closestNonBoss = null;
        float minBossDistSquared = float.MaxValue;
        float minNonBossDistSquared = float.MaxValue;

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            float distSquared = Vector2.DistanceSquared(origin, npc.Center);
            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
            {
                if (distSquared < minBossDistSquared || distSquared == minBossDistSquared && npc.whoAmI < closestBoss?.whoAmI)
                {
                    minBossDistSquared = distSquared;
                    closestBoss = npc;
                }
            }
            else
            {
                if (distSquared < minNonBossDistSquared || distSquared == minNonBossDistSquared && npc.whoAmI < closestNonBoss?.whoAmI)
                {
                    minNonBossDistSquared = distSquared;
                    closestNonBoss = npc;
                }
            }
        }

        return data.BossPriority && closestBoss != null ? closestBoss : closestNonBoss;
    }

    public static bool TryGetClosestNPC(NPCSeekingData data, out NPC target)
    {
        target = GetClosestNPC(data);
        if (!target.CanHomeInto())
        {
            return false;
        }

        return true;
    }

    public static List<NPC> GetNPCsFarthestToClosest(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<NPC> bosses = [];
        List<NPC> nonBosses = [];

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
                bosses.Add(npc);
            else
                nonBosses.Add(npc);
        }

        List<NPC> validNPCs = data.BossPriority && bosses.Count > 0 ? bosses : nonBosses;
        validNPCs.Sort((a, b) =>
        {
            float distA = Vector2.DistanceSquared(origin, a.Center);
            float distB = Vector2.DistanceSquared(origin, b.Center);
            int distCompare = distB.CompareTo(distA);
            return distCompare != 0 ? distCompare : a.whoAmI.CompareTo(b.whoAmI);
        });

        return validNPCs;
    }

    public static NPC GetFarthestNPC(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        NPC farthestBoss = null;
        NPC farthestNonBoss = null;
        float maxBossDistSquared = -1f;
        float maxNonBossDistSquared = -1f;

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            float distSquared = Vector2.DistanceSquared(origin, npc.Center);
            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
            {
                if (distSquared > maxBossDistSquared || distSquared == maxBossDistSquared && npc.whoAmI < farthestBoss?.whoAmI)
                {
                    maxBossDistSquared = distSquared;
                    farthestBoss = npc;
                }
            }
            else
            {
                if (distSquared > maxNonBossDistSquared || distSquared == maxNonBossDistSquared && npc.whoAmI < farthestNonBoss?.whoAmI)
                {
                    maxNonBossDistSquared = distSquared;
                    farthestNonBoss = npc;
                }
            }
        }

        return data.BossPriority && farthestBoss != null ? farthestBoss : farthestNonBoss;
    }

    public static NPC GetStrongestNPC(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        NPC strongestBoss = null;
        NPC strongestNonBoss = null;
        int maxBossHealth = -1;
        int maxNonBossHealth = -1;

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
            {
                if (npc.life > maxBossHealth || npc.life == maxBossHealth && npc.whoAmI < strongestBoss?.whoAmI)
                {
                    maxBossHealth = npc.life;
                    strongestBoss = npc;
                }
            }
            else
            {
                if (npc.life > maxNonBossHealth || npc.life == maxNonBossHealth && npc.whoAmI < strongestNonBoss?.whoAmI)
                {
                    maxNonBossHealth = npc.life;
                    strongestNonBoss = npc;
                }
            }
        }

        return data.BossPriority && strongestBoss != null ? strongestBoss : strongestNonBoss;
    }

    public static NPC GetWeakestNPC(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        NPC weakestBoss = null;
        NPC weakestNonBoss = null;
        int minBossHealth = int.MaxValue;
        int minNonBossHealth = int.MaxValue;

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
            {
                if (npc.life < minBossHealth || npc.life == minBossHealth && npc.whoAmI < weakestBoss?.whoAmI)
                {
                    minBossHealth = npc.life;
                    weakestBoss = npc;
                }
            }
            else
            {
                if (npc.life < minNonBossHealth || npc.life == minNonBossHealth && npc.whoAmI < weakestNonBoss?.whoAmI)
                {
                    minNonBossHealth = npc.life;
                    weakestNonBoss = npc;
                }
            }
        }

        return data.BossPriority && weakestBoss != null ? weakestBoss : weakestNonBoss;
    }

    public static NPC GetNPCInLargestCluster(NPCSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<NPC> bosses = new();
        List<NPC> nonBosses = new();

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
                bosses.Add(npc);
            else
                nonBosses.Add(npc);
        }

        List<NPC> validNPCs = data.BossPriority && bosses.Count > 0 ? bosses : nonBosses;
        if (validNPCs.Count == 0)
            return null;

        NPC bestNPC = null;
        int maxNeighbors = -1;
        float clusterRadiusSquared = 100f * 100f;

        foreach (NPC npc in validNPCs)
        {
            int neighbors = 0;
            foreach (NPC other in validNPCs)
            {
                if (npc != other && Vector2.DistanceSquared(npc.Center, other.Center) <= clusterRadiusSquared)
                    neighbors++;
            }

            if (neighbors > maxNeighbors || neighbors == maxNeighbors && npc.whoAmI < bestNPC?.whoAmI)
            {
                maxNeighbors = neighbors;
                bestNPC = npc;
            }
        }

        return bestNPC;
    }

    public static NPC GetLastNPCInLinearFormation(NPCSeekingData data, int minThreshold)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<NPC> bosses = [];
        List<NPC> nonBosses = [];

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc, origin, radiusSquared, data))
                continue;

            if (data.BossPriority && (npc.boss || npc.type == NPCID.WallofFleshEye))
                bosses.Add(npc);
            else
                nonBosses.Add(npc);
        }

        List<NPC> validNPCs = data.BossPriority && bosses.Count > 0 ? bosses : nonBosses;
        if (validNPCs.Count < minThreshold)
            return null;

        NPC bestLastNPC = null;
        int maxInLine = 0;
        float tolerance = 50f;

        for (int i = 0; i < validNPCs.Count; i++)
        {
            NPC start = validNPCs[i];
            for (int j = i + 1; j < validNPCs.Count; j++)
            {
                NPC end = validNPCs[j];
                Vector2 direction = Vector2.Normalize(end.Center - start.Center);
                int inLine = 2;

                for (int k = 0; k < validNPCs.Count; k++)
                {
                    if (k == i || k == j)
                        continue;
                    NPC mid = validNPCs[k];
                    Vector2 toMid = mid.Center - start.Center;
                    float projection = Vector2.Dot(toMid, direction);
                    Vector2 closestPoint = start.Center + direction * projection;
                    if (Vector2.DistanceSquared(mid.Center, closestPoint) <= tolerance * tolerance)
                        inLine++;
                }

                if (inLine >= minThreshold && inLine > maxInLine)
                {
                    maxInLine = inLine;
                    bestLastNPC = end;
                }
            }
        }

        if (bestLastNPC != null)
        {
            NPC firstInLine = null;
            float minDist = float.MaxValue;
            foreach (NPC npc in validNPCs)
            {
                float dist = Vector2.DistanceSquared(npc.Center, origin);
                if (dist < minDist)
                {
                    minDist = dist;
                    firstInLine = npc;
                }
            }

            Vector2 lineDir = Vector2.Normalize(bestLastNPC.Center - firstInLine.Center);
            Vector2 toOrigin = origin - firstInLine.Center;
            float proj = Vector2.Dot(toOrigin, lineDir);
            Vector2 closest = firstInLine.Center + lineDir * proj;
            if (Vector2.DistanceSquared(origin, closest) > tolerance * tolerance)
                return null;
        }

        return bestLastNPC;
    }
}

public static class ProjectileTargeting
{
    /// <param name="Origin">Where to start at</param>
    /// <param name="Radius">Maximum radius to account for</param>
    /// <param name="LOS">Check for line of sight?</param>
    /// <param name="Type">When a type is specified, the method will only look for this projectile</param>
    /// <param name="ExemptTargets">Blacklist certain projectiles</param>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct ProjectileSeekingData(Vector2 Origin, int Radius, bool LOS = false, int Type = int.MinValue, List<Projectile> ExemptTargets = null)
    {
        public override readonly string ToString()
        {
            return $"Origin={Origin}, Radius={Radius}, Line of Sight? {LOS}, Type={Type}, Exemptions={(ExemptTargets != null ? string.Join(", ", ExemptTargets) : "null")}";
        }
    }

    private static bool IsValidTarget(Projectile proj, Vector2 origin, float radiusSquared, ProjectileSeekingData data)
    {
        return proj.active &&
               (data.Type == int.MinValue || proj.type == data.Type) &&
               Vector2.DistanceSquared(origin, proj.Center) <= radiusSquared &&
               (data.ExemptTargets == null || !data.ExemptTargets.Contains(proj)) &&
               (!data.LOS || Collision.CanHit(origin, 1, 1, proj.Center, 1, 1));
    }

    private static bool IsPriorityProjectile(Projectile proj, int type)
    {
        if (type == int.MinValue)
            return false;

        return proj.type == type;
    }

    public static List<Projectile> GetProjectilesClosestToFarthest(ProjectileSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<Projectile> validProjectiles = [];

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (!IsValidTarget(proj, origin, radiusSquared, data))
                continue;
            validProjectiles.Add(proj);
        }

        validProjectiles.Sort((a, b) =>
        {
            bool aIsPriority = IsPriorityProjectile(a, data.Type);
            bool bIsPriority = IsPriorityProjectile(b, data.Type);
            if (aIsPriority != bIsPriority)
                return aIsPriority ? -1 : 1;
            float distA = Vector2.DistanceSquared(origin, a.Center);
            float distB = Vector2.DistanceSquared(origin, b.Center);
            int distCompare = distA.CompareTo(distB);
            return distCompare != 0 ? distCompare : a.whoAmI.CompareTo(b.whoAmI);
        });

        return validProjectiles;
    }

    public static Projectile GetClosestProjectile(ProjectileSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        Projectile closest = null;
        float minDistSquared = float.MaxValue;

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (!IsValidTarget(proj, origin, radiusSquared, data))
                continue;

            float distSquared = Vector2.DistanceSquared(origin, proj.Center);
            if (distSquared < minDistSquared || distSquared == minDistSquared && IsPriorityProjectile(proj, data.Type) && !IsPriorityProjectile(closest, data.Type) ||
                distSquared == minDistSquared && IsPriorityProjectile(closest, data.Type) == IsPriorityProjectile(proj, data.Type) && proj.whoAmI < closest.whoAmI)
            {
                minDistSquared = distSquared;
                closest = proj;
            }
        }

        return closest;
    }

    public static List<Projectile> GetProjectilesFarthestToClosest(ProjectileSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<Projectile> validProjectiles = [];

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (!IsValidTarget(proj, origin, radiusSquared, data))
                continue;
            validProjectiles.Add(proj);
        }

        validProjectiles.Sort((a, b) =>
        {
            bool aIsPriority = IsPriorityProjectile(a, data.Type);
            bool bIsPriority = IsPriorityProjectile(b, data.Type);
            if (aIsPriority != bIsPriority)
                return aIsPriority ? -1 : 1;
            float distA = Vector2.DistanceSquared(origin, a.Center);
            float distB = Vector2.DistanceSquared(origin, b.Center);
            int distCompare = distB.CompareTo(distA);
            return distCompare != 0 ? distCompare : a.whoAmI.CompareTo(b.whoAmI);
        });

        return validProjectiles;
    }

    public static Projectile GetFarthestProjectile(ProjectileSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        Projectile farthest = null;
        float maxDistSquared = -1f;

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (!IsValidTarget(proj, origin, radiusSquared, data))
                continue;

            float distSquared = Vector2.DistanceSquared(origin, proj.Center);
            if (distSquared > maxDistSquared || distSquared == maxDistSquared && IsPriorityProjectile(proj, data.Type) && !IsPriorityProjectile(farthest, data.Type) ||
                distSquared == maxDistSquared && IsPriorityProjectile(farthest, data.Type) == IsPriorityProjectile(proj, data.Type) && proj.whoAmI < farthest.whoAmI)
            {
                maxDistSquared = distSquared;
                farthest = proj;
            }
        }

        return farthest;
    }

    public static Projectile GetProjectileInLargestCluster(ProjectileSeekingData data)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        List<Projectile> validProjectiles = [];

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (IsValidTarget(proj, origin, radiusSquared, data))
                validProjectiles.Add(proj);
        }

        if (validProjectiles.Count == 0)
            return null;

        Projectile bestProj = null;
        int maxNeighbors = -1;
        float clusterRadiusSquared = 100f * 100f;

        foreach (Projectile proj in validProjectiles)
        {
            int neighbors = 0;
            foreach (Projectile other in validProjectiles)
            {
                if (proj != other && Vector2.DistanceSquared(proj.Center, other.Center) <= clusterRadiusSquared)
                    neighbors++;
            }

            if (neighbors > maxNeighbors || neighbors == maxNeighbors && IsPriorityProjectile(proj, data.Type) && !IsPriorityProjectile(bestProj, data.Type) ||
                neighbors == maxNeighbors && IsPriorityProjectile(bestProj, data.Type) == IsPriorityProjectile(proj, data.Type) && proj.whoAmI < bestProj.whoAmI)
            {
                maxNeighbors = neighbors;
                bestProj = proj;
            }
        }

        return bestProj;
    }
}

public static class PlayerTargeting
{
    /// <param name="Origin">Where to start at</param>
    /// <param name="Radius">Maximum radius to account for</param>
    /// <param name="LOS">Check for line of sight?</param>
    /// <param name="ExemptTargets">Blacklist certain players</param>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct PlayerSeekingData(Vector2 Origin, int Radius, bool LOS = false, List<Player> ExemptTargets = null)
    {
        public override readonly string ToString()
        {
            return $"Origin={Origin}, Radius={Radius}, Line of Sight? {LOS},  Exemptions={(ExemptTargets != null ? string.Join(", ", ExemptTargets) : "null")}";
        }
    }

    private static bool IsValidTarget(Player player, Vector2 origin, float radiusSquared, PlayerSeekingData data)
    {
        return player != null && player.active && !player.dead &&
               Vector2.DistanceSquared(origin, player.Center) <= radiusSquared &&
               (data.ExemptTargets == null || !data.ExemptTargets.Contains(player)) &&
               (!data.LOS || Collision.CanHit(origin, 1, 1, player.Center, 1, 1));
    }

    public static List<Player> ClosestPlayers(PlayerSeekingData data)
    {
        List<Player> players = [];
        float radiusSquared = data.Radius * data.Radius;

        foreach (Player player in Main.ActivePlayers)
        {
            if (!IsValidTarget(player, data.Origin, radiusSquared, data))
                continue;

            players.Add(player);
        }

        if (players.Count == 0)
            return players;

        players.Sort((a, b) =>
        {
            float distA = Vector2.DistanceSquared(a.Center, data.Origin);
            float distB = Vector2.DistanceSquared(b.Center, data.Origin);
            int distCompare = distA.CompareTo(distB);
            return distCompare != 0 ? distCompare : a.whoAmI.CompareTo(b.whoAmI);
        });

        return players;
    }

    /// <summary>
    /// Finds the nearest player available regardless of anything
    /// </summary>
    /// <param name="position">The position to start at</param>
    /// <returns>A player, if any</returns>
    public static Player FindNearestPlayer(Vector2 position)
    {
        Player plr = null;

        for (int k = 0; k < Main.maxPlayers; k++)
        {
            if (Main.player[k] != null && Main.player[k].active && (plr == null || Vector2.DistanceSquared(position, Main.player[k].Center) < Vector2.DistanceSquared(position, plr.Center)))
                plr = Main.player[k];
        }
        return plr;
    }

    public static bool TryFindClosestPlayer(PlayerSeekingData data, out Player target)
    {
        Vector2 origin = data.Origin;
        float radiusSquared = data.Radius * data.Radius;
        Player closest = null;
        float minPlayerDistSquared = float.MaxValue;

        foreach (Player player in Main.ActivePlayers)
        {
            if (!IsValidTarget(player, origin, radiusSquared, data))
                continue;

            float distSquared = Vector2.DistanceSquared(origin, player.Center);
            if (distSquared < minPlayerDistSquared || distSquared == minPlayerDistSquared && player.whoAmI < closest?.whoAmI)
            {
                minPlayerDistSquared = distSquared;
                closest = player;
            }
        }

        if (closest == null)
        {
            target = null;
            return false;
        }

        target = closest;
        return true;
    }

    /// <summary>
    /// Searches for players and NPC's and updates the target accordingly
    /// </summary>
    /// <param name="npc">This <see cref="NPC"/></param>
    /// <param name="target">Most typically <see cref="NPC.GetTargetData(bool)"/></param>
    public static void SearchForTarget(this NPC npc, NPCAimedTarget target)
    {
        if (!target.Invalid)
            return;

        NPCUtils.TargetSearchResults targetSearchResults = NPCUtils.SearchForTarget(npc, NPCUtils.TargetSearchFlag.NPCs | NPCUtils.TargetSearchFlag.Players);

        if (!targetSearchResults.FoundTarget)
            return;

        // Check for players.
        npc.target = targetSearchResults.NearestTargetIndex;
        npc.targetRect = targetSearchResults.NearestTargetHitbox;
    }

    public static bool SearchForPlayerTarget(this NPC npc, out Player target, bool faceTarget = true)
    {
        if (npc.target < 0 || npc.target > Main.maxPlayers || !npc.HasValidTarget)
        {
            float distance = 0f;
            float realDist = 0f;
            bool t = false;
            int tankTarget = -1;
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.dead && !player.ghost)
                    npc.TryTrackingTarget(ref distance, ref realDist, ref t, ref tankTarget, player.whoAmI);
            }
            npc.SetTargetTrackingValues(faceTarget, realDist, tankTarget);
        }

        if (npc.target >= 0 && npc.target < Main.maxPlayers && npc.HasValidTarget)
        {
            target = Main.player[npc.target];
            return true;
        }

        target = null;
        return false;
    }
}
#endregion