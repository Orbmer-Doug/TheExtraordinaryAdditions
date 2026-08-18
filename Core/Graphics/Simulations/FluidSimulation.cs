using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Core.Graphics.Simulations;

#region Simulation Data

/// <summary>
///     Contains all configuration parameters that govern the behavior of a single <see cref="FluidSimulationHandle"/>
/// </summary>
public struct FluidSettings
{
    /// <summary>
    ///     The horizontal resolution of the simulation grid, in texels.
    ///     Larger values produce more detail but consume more VRAM and GPU time.
    /// </summary>
    /// <remarks>
    ///     Defaults to 256. Strongly recommended to keep as a power of two.
    /// </remarks>
    public int GridWidth = 256;

    /// <summary>
    ///     The vertical resolution of the simulation grid, in texels.
    ///     Larger values produce more detail but consume more VRAM and GPU time.
    /// </summary>
    /// <remarks>
    ///     Defaults to 256. Strongly recommended to keep as a power of two.
    /// </remarks>
    public int GridHeight = 256;

    /// <summary>
    ///     How many Jacobi relaxation passes are performed per frame when solving the pressure Poisson equation.
    /// </summary>
    /// <remarks>
    ///     Higher values produce a more divergence-free velocity field at the cost of additional GPU passes per frame.
    ///     Values between 2 and 8 are typical.
    ///     Defaults to 3.
    /// </remarks>
    public int DivergenceClearanceIterations = 3;

    /// <summary>
    ///     Controls how strongly the velocity field diffuses outward each frame.
    /// </summary>
    /// <remarks>
    ///     A value of 0 means no diffusion. Values above roughly 0.5 tend to make
    ///     the fluid look overly blurry. Defaults to 0.2.
    /// </remarks>
    public float DiffusionCoefficient = 0.2f;

    /// <summary>
    ///     A per-frame scalar multiplied against density after each advection step.
    ///     Controls how quickly density fades out naturally.
    /// </summary>
    /// <remarks>
    ///     Must be in the range (0, 1]. Values close to 1 produce long-lived density.
    ///     Values below roughly 0.9 will cause density to fade very quickly.
    ///     Defaults to 0.97.
    /// </remarks>
    public float DissipationDecayFactor = 0.97f;

    /// <summary>
    ///     Whether the velocity field should treat solid tiles as boundaries,
    ///     zeroing velocity at texels that correspond to solid tile positions.
    /// </summary>
    public bool CollidesWithTiles = true;

    /// <summary>
    ///     Strength of vorticity confinement applied after each velocity advection step.
    ///     Higher values produce tighter, more energetic swirling structures.
    /// </summary>
    /// <remarks>
    ///     A value of 0 disables vorticity confinement entirely. Values above roughly
    ///     0.8 can introduce instability. Defaults to 0.45.
    /// </remarks>
    public float Vorticity = 0.45f;

    /// <summary>
    ///     Magnitude of the random noise vector injected into the velocity field
    ///     each frame. Introduces organic turbulence without requiring explicit
    ///     fluid sources to drive it.
    /// </summary>
    /// <remarks>
    ///     A value of 0 disables noise injection entirely. Defaults to 0.15.
    /// </remarks>
    public float NoiseInjectionAcceleration = 0.15f;

    /// <summary>
    ///     An optional function evaluated once per frame. When it returns
    ///     <see langword="true"/>, the <see cref="FluidSimulationProcessor"/>
    ///     will automatically release the handle back to the pool without
    ///     requiring an explicit call
    /// </summary>
    public Func<bool> AutomaticDisposalFunction = null;

    public FluidSettings()
    {
    }
}

/// <summary>
///     Identifies which variant of a <see cref="FluidCommand"/> is active
/// </summary>
public enum FluidCommandType : byte
{
    Splat,
    OmnidirectionalSplat,
    GlobalForce
}

/// <summary>
///     A lightweight value-type descriptor representing a single deferred GPU
///     operation to be executed against a <see cref="FluidSimulationHandle"/> during the rendering pre-draw phase.
/// </summary>
/// <remarks>
///     Use the static factory methods rather than constructing this struct
///     directly, to ensure only the fields relevant to a given variant are populated.
/// </remarks>
public readonly struct FluidCommand
{
    /// <summary>
    ///     Which operation this command represents.
    /// </summary>
    public readonly FluidCommandType Type;

    // --- Splat fields ---

    /// <summary>
    ///     The brush texture whose silhouette defines the footprint of injected density and velocity.
    ///     Only used by <see cref="FluidCommandType.Splat"/>.
    /// </summary>
    public readonly Texture2D Brush;

    /// <summary>
    ///     How much density to inject at this splat location.
    ///     Only used by <see cref="FluidCommandType.Splat"/> and <see cref="FluidCommandType.OmnidirectionalSplat"/>.
    /// </summary>
    public readonly float Density;

    /// <summary>
    ///     The velocity to inject at this splat location, in grid-texels per frame.
    ///     Only used by <see cref="FluidCommandType.Splat"/>.
    /// </summary>
    public readonly Vector2 Velocity;

    /// <summary>
    ///     The position within the grid, in grid-space texels, at which to center this operation.
    ///     Used by <see cref="FluidCommandType.Splat"/> and <see cref="FluidCommandType.OmnidirectionalSplat"/>.
    /// </summary>
    public readonly Vector2 Position;

    /// <summary>
    ///     The source rectangle within <see cref="Brush"/> to sample from.
    ///     Only used by <see cref="FluidCommandType.Splat"/>.
    /// </summary>
    public readonly Rectangle? SourceRect;

    /// <summary>
    ///     The rotation of the brush, in radians.
    ///     Only used by <see cref="FluidCommandType.Splat"/>.
    /// </summary>
    public readonly float Rotation;

    /// <summary>
    ///     The origin within the brush texture around which rotation and scale are applied, in texels.
    ///     Only used by <see cref="FluidCommandType.Splat"/>.
    /// </summary>
    public readonly Vector2 Origin;

    /// <summary>
    ///     The scale applied to the brush before rendering.
    ///     Used by <see cref="FluidCommandType.Splat"/> and <see cref="FluidCommandType.OmnidirectionalSplat"/>.
    /// </summary>
    public readonly Vector2 Scale;

    // --- OmnidirectionalSplat fields ---

    /// <summary>
    ///     The magnitude of the outward radial velocity injected at each texel within the splat footprint.
    ///     Only used by <see cref="FluidCommandType.OmnidirectionalSplat"/>.
    /// </summary>
    public readonly float OutwardSpeed;

    // --- GlobalForce fields ---

    /// <summary>
    ///     The acceleration vector added uniformly to the entire velocity field, in grid-texels per frame squared.
    ///     Only used by <see cref="FluidCommandType.GlobalForce"/>.
    /// </summary>
    public readonly Vector2 Force;

    private FluidCommand(
        FluidCommandType type,
        Texture2D brush,
        float density,
        Vector2 velocity,
        Vector2 position,
        Rectangle? sourceRect,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        float outwardSpeed,
        Vector2 force)
    {
        Type = type;
        Brush = brush;
        Density = density;
        Velocity = velocity;
        Position = position;
        SourceRect = sourceRect;
        Rotation = rotation;
        Origin = origin;
        Scale = scale;
        OutwardSpeed = outwardSpeed;
        Force = force;
    }

    #region Factory

    /// <summary>
    ///     Creates a <see cref="FluidCommandType.Splat"/> command that injects
    ///     density and velocity using a brush texture as a shaped mask.
    /// </summary>
    /// <param name="brush">
    ///     The texture whose silhouette defines the injection footprint.
    /// </param>
    /// <param name="density">
    ///     How much density to inject. Typical values are in the range [0.1, 5].
    /// </param>
    /// <param name="velocity">
    ///     The velocity to inject, in grid-texels per frame.
    /// </param>
    /// <param name="position">
    ///     The center of the injection in grid-space texels.
    /// </param>
    /// <param name="sourceRect">
    ///     The region of <paramref name="brush"/> to sample.
    /// </param>
    /// <param name="rotation">
    ///     Rotation of the brush in radians.
    /// </param>
    /// <param name="origin">
    ///     The pivot point within <paramref name="brush"/>, in texels.
    /// </param>
    /// <param name="scale">
    ///     Scale applied to the brush before rendering onto the grid.
    /// </param>
    public static FluidCommand Splat(
        Texture2D brush,
        float density,
        Vector2 velocity,
        Vector2 position,
        Rectangle? sourceRect,
        float rotation,
        Vector2 origin,
        Vector2 scale)
    {
        return new FluidCommand(
            type: FluidCommandType.Splat,
            brush: brush,
            density: density,
            velocity: velocity,
            position: position,
            sourceRect: sourceRect,
            rotation: rotation,
            origin: origin,
            scale: scale,
            outwardSpeed: 0f,
            force: Vector2.Zero);
    }

    /// <summary>
    ///     Creates a <see cref="FluidCommandType.OmnidirectionalSplat"/> command that injects velocity radially outward from a center point.
    /// </summary>
    /// <param name="density">
    ///     How much density to inject at the center point.
    /// </param>
    /// <param name="outwardSpeed">
    ///     Magnitude of the outward radial velocity injected at each covered texel.
    /// </param>
    /// <param name="position">
    ///     The origin of the outward expansion in grid-space texels.
    /// </param>
    /// <param name="scale">
    ///     Scale of the affected region.
    /// </param>
    public static FluidCommand OmnidirectionalSplat(
        float density,
        float outwardSpeed,
        Vector2 position,
        Vector2 scale)
    {
        return new FluidCommand(
            type: FluidCommandType.OmnidirectionalSplat,
            brush: null,
            density: density,
            velocity: Vector2.Zero,
            position: position,
            sourceRect: default,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: scale,
            outwardSpeed: outwardSpeed,
            force: Vector2.Zero);
    }

    /// <summary>
    ///     Creates a <see cref="FluidCommandType.GlobalForce"/> command that adds a uniform acceleration to the entire velocity field.
    /// </summary>
    /// <param name="force">
    ///     The acceleration vector in grid-texels per frame squared.
    /// </param>
    public static FluidCommand GlobalForce(Vector2 force)
    {
        return new FluidCommand(
            type: FluidCommandType.GlobalForce,
            brush: null,
            density: 0f,
            velocity: Vector2.Zero,
            position: Vector2.Zero,
            sourceRect: default,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: Vector2.One,
            outwardSpeed: 0f,
            force: force);
    }

    #endregion
}

#endregion

#region Simulation Handle

/// <summary>
///     Represents an active lease on a single Navier-Stokes fluid simulation slot managed by <see cref="FluidSimulationProcessor"/>.
/// </summary>
/// <remarks>
///     <para>
///         Instances of this class are never constructed directly.
///         Obtain one by calling <see cref="FluidSimulationProcessor.RequestNew"/>.
///     </para>
///     <para>
///         All positional arguments use simulation grid space, where
///         <c>GridSize * 0.5f</c> is the center of the grid. Convert from world
///         space by subtracting <see cref="Center"/> and dividing by
///         <see cref="Scale"/>.
///     </para>
/// </remarks>
public sealed class FluidSimulationHandle
{
    // Command queue (double-buffered)
    //
    // pending is filled via the public API.
    // draining is consumed by the processor during rendering.
    private List<FluidCommand> _pendingCommands = [];
    private List<FluidCommand> _drainingCommands = [];

    #region Render Targets

    // The velocity+density field uses two targets for ping-pong advection.
    // The pressure solve uses two targets for Jacobi iteration ping-pong.
    // Divergence and curl each need exactly one target because they are only
    // ever written once per frame before being consumed.

    private RenderTarget2D[] _velocityDensityTargets;
    internal int VelocityWriteIndex { get; private set; }
    internal int VelocityReadIndex { get; private set; }

    private RenderTarget2D[] _pressureTargets;
    internal int PressureWriteIndex { get; private set; }
    internal int PressureReadIndex { get; private set; }

    private RenderTarget2D _divergenceTarget;
    private RenderTarget2D _curlTarget;

    /// <summary>
    ///     The combined velocity and density render target from the last completed simulation step.
    ///     This is what shaders should sample.
    /// </summary>
    public Texture2D VelocityDensityTarget => _velocityDensityTargets?[VelocityReadIndex];

    /// <summary>The velocity+density target the processor writes to this frame</summary>
    internal RenderTarget2D WriteVelocityDensity => _velocityDensityTargets[VelocityWriteIndex];

    /// <summary>The velocity+density target the processor reads from this frame</summary>
    internal RenderTarget2D ReadVelocityDensity => _velocityDensityTargets[VelocityReadIndex];

    /// <summary>The pressure target the processor writes to during Jacobi iteration</summary>
    internal RenderTarget2D WritePressure => _pressureTargets[PressureWriteIndex];

    /// <summary>The pressure target the processor reads from during Jacobi iteration</summary>
    internal RenderTarget2D ReadPressure => _pressureTargets[PressureReadIndex];

    /// <summary>
    ///     The single-frame divergence target.
    ///     Written once per frame by the divergence pass, then read by every Jacobi iteration pass.
    /// </summary>
    internal RenderTarget2D DivergenceTarget => _divergenceTarget;

    /// <summary>
    ///     The single-frame curl magnitude target.
    ///     Written once by the vorticity pass, then read by the confinement pass.
    /// </summary>
    internal RenderTarget2D CurlTarget => _curlTarget;

    #endregion

    #region State

    /// <summary>
    ///     Whether this handle is currently leased to an owner and actively simulating.
    /// </summary>
    public bool InUse { get; private set; }

    /// <summary>
    ///     The world-space center position of the simulation grid.
    ///     Set this every frame to keep the simulation anchored to a moving entity.
    /// </summary>
    public Vector2 Center { get; set; }

    /// <summary>
    ///     A grid-space offset to the advection. Used to make the fluid seem
    ///     like it is in the world whilst the target is fixed.
    /// </summary>
    public Vector2 Delta { get; set; }

    /// <summary>
    ///     The configuration that governs this simulation's behavior.
    /// </summary>
    public FluidSettings Settings { get; private set; }

    /// <summary>
    ///     World-space pixels per grid texel. Determines how large the simulation appears in the world relative to its grid resolution.
    /// </summary>
    public float Scale { get; private set; }

    /// <summary>
    ///     The dimensions of the simulation grid in texels, as a <see cref="Vector2"/> for convenient arithmetic with world positions.
    /// </summary>
    public Vector2 GridSize => new(Settings.GridWidth, Settings.GridHeight);

    /// <summary>
    ///     How many additional simulation steps have been requested this frame
    ///     via <see cref="ForceUpdate"/>. Reset to zero by the processor after
    ///     all extra steps are executed.
    /// </summary>
    internal int PendingExtraUpdates { get; private set; }

    /// <summary>
    ///     When <see langword="true"/>, the processor will clear all targets to
    ///     zero before executing the first simulation step. Set automatically
    ///     when a handle is leased, and cleared after the processor flushes.
    /// </summary>
    internal bool NeedsInitialClear { get; private set; }

    #endregion

    #region Internal Lifecycle

    /// <summary>
    ///     Activates this handle for a new owner. Called by the processor when
    ///     fulfilling a <see cref="FluidSimulationProcessor.RequestNew"/> call.
    /// </summary>
    internal void Lease(float scale, FluidSettings settings)
    {
        Scale = scale;
        Settings = settings;
        InUse = true;
        VelocityReadIndex = 0;
        VelocityWriteIndex = 1;
        PressureReadIndex = 0;
        PressureWriteIndex = 1;
        PendingExtraUpdates = 0;
        NeedsInitialClear = true;
        Center = Vector2.Zero;

        EnsureTargets();
    }

    /// <summary>
    ///     Returns this handle to the free pool. Called by the processor, either
    ///     when <see cref="FluidSimulationProcessor.Release"/> is invoked or when
    ///     <see cref="FluidSettings.AutomaticDisposalFunction"/> returns <see langword="true"/>.
    /// </summary>
    internal void ReturnToPool()
    {
        InUse = false;
        PendingExtraUpdates = 0;
        NeedsInitialClear = false;

        // Discard any commands queued by the outgoing owner so they do not bleed into a future owner's simulation.
        _pendingCommands.Clear();
        _drainingCommands.Clear();
    }

    /// <summary>
    ///     Swaps the ping-pong indices so that the write target of the current frame becomes the read target of the next frame.
    /// </summary>
    internal void SwapVelocityPingPong()
    {
        (VelocityReadIndex, VelocityWriteIndex) = (VelocityWriteIndex, VelocityReadIndex);
    }

    internal void SwapPressurePingPong()
    {
        (PressureReadIndex, PressureWriteIndex) = (PressureWriteIndex, PressureReadIndex);
    }

    /// <summary>
    ///     Atomically swaps the pending and draining command lists, then returns
    ///     the list now containing the commands that were enqueued since the last drain.
    /// </summary>
    internal List<FluidCommand> SwapCommandBuffer()
    {
        // After the swap:
        //   pending  = old draining list, cleared and ready for new commands.
        //   draining = old pending list, full of commands for rendering.
        (_pendingCommands, _drainingCommands) = (_drainingCommands, _pendingCommands);
        _pendingCommands.Clear();

        return _drainingCommands;
    }

    /// <summary>
    ///     Signals that the processor has finished the initial clear and normal simulation can begin.
    /// </summary>
    internal void AcknowledgeInitialClear() => NeedsInitialClear = false;

    /// <summary>
    ///     Decrements the pending extra update counter by one.
    ///     Called by the processor after executing each extra step.
    /// </summary>
    internal void AcknowledgeExtraUpdate() => PendingExtraUpdates--;

    #endregion

    #region Target Management

    /// <summary>
    ///     Creates or recreates render targets to match the current <see cref="Settings"/> grid dimensions.
    /// </summary>
    /// <remarks>
    ///     Must be called on the main thread.
    ///     Skips allocation if the existing targets already have the correct dimensions.
    /// </remarks>
    internal void EnsureTargets()
    {
        int w = Settings.GridWidth;
        int h = Settings.GridHeight;

        bool dimensionsChanged =
            _velocityDensityTargets?[0] is null ||
            _velocityDensityTargets[0].IsDisposed ||
            _velocityDensityTargets[0].Width != w ||
            _velocityDensityTargets[0].Height != h;

        if (!dimensionsChanged)
            return;

        DisposeTargets();

        GraphicsDevice gd = Main.instance.GraphicsDevice;

        _velocityDensityTargets = new RenderTarget2D[2];
        _pressureTargets = new RenderTarget2D[2];

        const SurfaceFormat format = SurfaceFormat.HalfVector4;
        for (int i = 0; i < 2; i++)
        {
            _velocityDensityTargets[i] = new RenderTarget2D(
                gd, w, h, false,
                format,
                DepthFormat.None);

            _pressureTargets[i] = new RenderTarget2D(
                gd, w, h, false,
                format,
                DepthFormat.None);
        }

        _divergenceTarget = new RenderTarget2D(
            gd, w, h, false,
            format,
            DepthFormat.None);

        _curlTarget = new RenderTarget2D(
            gd, w, h, false,
            format,
            DepthFormat.None);
    }

    /// <summary>
    ///     Disposes all render targets held by this handle, freeing VRAM.
    /// </summary>
    internal void DisposeTargets()
    {
        if (_velocityDensityTargets is not null)
        {
            foreach (RenderTarget2D t in _velocityDensityTargets)
                t?.Dispose();
            _velocityDensityTargets = null;
        }

        if (_pressureTargets is not null)
        {
            foreach (RenderTarget2D t in _pressureTargets)
                t?.Dispose();
            _pressureTargets = null;
        }

        _divergenceTarget?.Dispose();
        _divergenceTarget = null;

        _curlTarget?.Dispose();
        _curlTarget = null;
    }

    #endregion

    #region Public API

    /// <summary>
    ///     Queues an injection of density and velocity using a brush texture as a shaped mask
    /// </summary>
    /// <param name="brush">
    ///     The texture whose silhouette defines the injection footprint.
    /// </param>
    /// <param name="density">
    ///     How much density to inject. Typical values are in the range [0.1, 5].
    /// </param>
    /// <param name="velocity">
    ///     The velocity to inject at this location, in grid-texels per frame.
    /// </param>
    /// <param name="position">
    ///     The center of the injection in grid-space texels, relative to the
    ///     top-left corner of the grid. Use <c>GridSize * 0.5f</c> for center.
    /// </param>
    /// <param name="sourceRect">
    ///     The region of <paramref name="brush"/> to sample.
    /// </param>
    /// <param name="rotation">
    ///     Rotation of the brush in radians.
    /// </param>
    /// <param name="origin">
    ///     The pivot point within <paramref name="brush"/>, in texels.
    /// </param>
    /// <param name="scale">
    ///     Scale applied to the brush footprint in grid space.
    /// </param>
    public void DrawOnCanvas(
        Texture2D brush,
        float density,
        Vector2 velocity,
        Vector2 position,
        Rectangle? sourceRect,
        float rotation,
        Vector2 origin,
        Vector2 scale)
    {
        _pendingCommands.Add(FluidCommand.Splat(
            brush, density, velocity, position, sourceRect, rotation, origin, scale));
    }

    /// <summary>
    ///     Queues an injection of radially outward velocity from a center point, optionally accompanied by density injection.
    /// </summary>
    /// <param name="density">
    ///     How much density to inject at the center point.
    /// </param>
    /// <param name="outwardSpeed">
    ///     Magnitude of the outward velocity injected at each texel within the affected region.
    /// </param>
    /// <param name="position">
    ///     The origin of the expansion in grid-space texels.
    /// </param>
    /// <param name="scale">
    ///     Scale of the affected region on the grid.
    /// </param>
    public void DrawOmnidirectional(
        float density,
        float outwardSpeed,
        Vector2 position,
        Vector2 scale)
    {
        _pendingCommands.Add(FluidCommand.OmnidirectionalSplat(
            density, outwardSpeed, position, scale));
    }

    /// <summary>
    ///     Queues a uniform acceleration applied to every texel of the velocity field this frame.
    /// </summary>
    /// <param name="force">
    ///     The acceleration vector in grid-texels per frame squared.
    /// </param>
    public void AddForce(Vector2 force)
    {
        _pendingCommands.Add(FluidCommand.GlobalForce(force));
    }

    /// <summary>
    ///     Requests one additional full simulation step to be executed this frame, beyond the one that runs automatically.
    /// </summary>
    /// <remarks>
    ///     Typically used alongside things analogous to <c>Projectile.MaxUpdates</c>.
    ///     For example, when a projectile runs multiple AI updates per game frame, call this once per
    ///     extra update so the simulation stays temporally consistent with the projectile's perceived speed.
    /// </remarks>
    public void ForceUpdate() => PendingExtraUpdates++;

    #endregion
}

#endregion

#region Processor

/// <summary>
///     A <see cref="FluidSimulationHandle"/> that owns the pool of
///     <see cref="FluidSimulationHandle.VelocityDensityTarget"/> instances, subscribes to the pre-draw
///     render target update loop, and executes the full Navier-Stokes pipeline
///     for every active handle each frame.
/// </summary>
/// <remarks>
///     <para>
///         The simulation pipeline executes in the following order per handle:
///         <list type="number">
///             <item>Splat injection</item>
///             <item>Semi-Lagrangian advection of velocity and density</item>
///             <item>Vorticity confinement (curl computation + force application)</item>
///             <item>Divergence computation</item>
///             <item>Jacobi pressure solve (N iterations)</item>
///             <item>Pressure gradient projection (divergence-free correction)</item>
///             <item>Tile boundary zeroing (optional)</item>
///         </list>
///     </para>
///     <para>
///         After all phases, <see cref="FluidSimulationHandle"/>
///         holds the fully resolved field, ready to be sampled by a rendering shader.
///     </para>
/// </remarks>
public sealed class FluidSimulationProcessor : ModSystem
{
    /// <summary>
    ///     The active singleton instance of this processor.
    /// </summary>
    public static FluidSimulationProcessor Instance => ModContent.GetInstance<FluidSimulationProcessor>();

    #region Constants

    /// <summary>
    ///     The maximum number of simultaneous fluid simulation instances the pool supports
    /// </summary>
    public const int PoolSize = 32;

    /// <summary>
    ///     The maximum absolute velocity magnitude, in grid texels per frame, that the simulation clamps to after advection to prevent runaways.
    /// </summary>
    public const float MaxVelocity = 116f;

    #endregion

    #region Loading

    private readonly FluidSimulationHandle[] _pool = new FluidSimulationHandle[PoolSize];

    public override void OnModLoad()
    {
        for (int i = 0; i < PoolSize; i++)
            _pool[i] = new FluidSimulationHandle();

        RenderTargetManager.RenderTargetUpdateLoopEvent += UpdateAllSimulations;
    }

    public override void OnModUnload()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent -= UpdateAllSimulations;

        Main.QueueMainThreadAction(() =>
        {
            foreach (FluidSimulationHandle handle in _pool)
                handle.DisposeTargets();
        });
    }

    #endregion

    #region Public API

    /// <summary>
    ///     Leases a handle from the free pool and initializes it with the given configuration.
    /// </summary>
    /// <param name="scale">
    ///     World-space pixels per grid texel. Controls how large the simulation
    ///     appears in the world relative to its grid resolution.
    /// </param>
    /// <param name="settings">
    ///     Configuration governing this simulation's behavior.
    /// </param>
    /// <returns>
    ///     An active <see cref="FluidSimulationHandle"/>, or
    ///     <see langword="null"/> if all pool slots are occupied.
    /// </returns>
    public FluidSimulationHandle RequestNew(float scale, FluidSettings settings)
    {
        foreach (FluidSimulationHandle handle in _pool)
        {
            if (handle.InUse)
                continue;

            handle.Lease(scale, settings);
            return handle;
        }

        Mod.Logger.Warn(
            "[FluidSimulationProcessor] Pool exhausted: all 32 simulation slots are in use. " +
            "The requesting simulation will not run.");
        return null;
    }

    /// <summary>
    ///     Returns a leased handle back to the free pool.
    ///     Safe to call if the handle is already released or <see langword="null"/>.
    /// </summary>
    public static void Release(FluidSimulationHandle handle)
    {
        if (handle is not null && handle.InUse)
            handle.ReturnToPool();
    }

    #endregion

    #region Main Updates

    private void UpdateAllSimulations()
    {
        if (Main.dedServ || Main.gameMenu)
            return;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        SpriteBatch sb = Main.spriteBatch;

        foreach (FluidSimulationHandle handle in _pool)
        {
            if (!handle.InUse)
                continue;

            // Check whether the simulation's owner has flagged it for automatic disposal.
            if (handle.Settings.AutomaticDisposalFunction?.Invoke() == true)
            {
                handle.ReturnToPool();
                continue;
            }

            StepSimulation(handle, gd, sb);

            // The owner may request additional steps to keep pace with a projectile
            // running multiple AI updates per game frame (Projectile.MaxUpdates).
            while (handle.PendingExtraUpdates > 0)
            {
                StepSimulation(handle, gd, sb);
                handle.AcknowledgeExtraUpdate();
            }
        }

        // Restore the screen as the active render target for the rest of the frame.
        gd.SetRenderTarget(null);
    }

    private void StepSimulation(FluidSimulationHandle handle, GraphicsDevice gd, SpriteBatch sb)
    {
        // On the first frame this handle is leased, flush all targets to zero.
        // Leftover data from a previous owner would immediately corrupt the simulation.
        if (handle.NeedsInitialClear)
        {
            ClearAllTargets(handle, gd);
            handle.AcknowledgeInitialClear();
        }

        // Separate the command buffer into splat-type commands (need GPU draws) and
        // global force commands (accumulated on the CPU and passed as a uniform).
        List<FluidCommand> commands = handle.SwapCommandBuffer();
        Vector2 accumulatedForce = Vector2.Zero;
        bool hasSplats = false;

        foreach (FluidCommand cmd in commands)
        {
            switch (cmd.Type)
            {
                case FluidCommandType.GlobalForce:
                    accumulatedForce += cmd.Force;
                    break;
                default:
                    hasSplats = true;
                    break;
            }
        }

        // Inject a small random impulse each frame to drive organic turbulence.
        if (handle.Settings.NoiseInjectionAcceleration > 0f)
        {
            accumulatedForce +=
                Main.rand.NextVector2Circular(1f, 1f) * handle.Settings.NoiseInjectionAcceleration;
        }

        // --- Phase 1: Splat injection ---
        // Brush-shaped and omnidirectional density/velocity injections are drawn
        // additively onto a copy of the current read field.
        if (hasSplats)
        {
            ExecuteSplatPhase(handle, commands, gd, sb);
            handle.SwapVelocityPingPong();
        }

        // --- Phase 2: Advection ---
        // Semi-Lagrangian back-trace of velocity and density. Global force and
        // dissipation are applied during this pass.
        ExecuteAdvect(handle, accumulatedForce, gd, sb);
        handle.SwapVelocityPingPong();

        if (handle.Settings.DiffusionCoefficient > 0f)
        {
            ExecuteDiffuse(handle, gd, sb);
        }

        // --- Phase 3: Vorticity confinement ---
        // Amplifies small-scale swirling structures that semi-Lagrangian advection
        // would otherwise damp out.
        if (handle.Settings.Vorticity > 0f)
        {
            ExecuteComputeCurl(handle, gd, sb);
            ExecuteApplyConfinement(handle, gd, sb);
            handle.SwapVelocityPingPong();
        }

        // --- Phase 4: Divergence ---
        // Compute the divergence of the velocity field into a dedicated target.
        // This target is read by every subsequent Jacobi iteration.
        ExecuteDivergence(handle, gd, sb);

        // --- Phase 5: Pressure solve ---
        // Jacobi relaxation solves the pressure Poisson equation. Both pressure
        // targets are cleared to zero at the start so the solver has a clean initial
        // estimate on every frame.
        ClearPressureTargets(handle, gd);
        for (int i = 0; i < handle.Settings.DivergenceClearanceIterations; i++)
        {
            ExecuteJacobi(handle, gd, sb);
            handle.SwapPressurePingPong();
        }
        // After N swaps, ReadPressure holds the most recent pressure estimate
        // regardless of whether N is even or odd.

        // --- Phase 6: Projection ---
        // Subtract the pressure gradient from velocity to produce a
        // divergence-free (incompressible) field. Density is passed through.
        ExecuteProject(handle, gd, sb);
        handle.SwapVelocityPingPong();

        // --- Phase 7: Tile boundary ---
        // Zero velocity at texels that correspond to solid tile positions.
        if (handle.Settings.CollidesWithTiles)
        {
            ExecuteBoundary(handle, gd, sb);
            handle.SwapVelocityPingPong();
        }
    }

    #endregion

    #region Phases

    /// <summary>
    ///     Copies <see cref="FluidSimulationHandle.ReadVelocityDensity"/> into
    ///     <see cref="FluidSimulationHandle.WriteVelocityDensity"/>, then draws
    ///     all queued splat commands additively on top.
    /// </summary>
    /// <remarks>
    ///     After this method returns, the caller must call
    ///     <see cref="FluidSimulationHandle.SwapVelocityPingPong"/> so that the
    ///     injected field becomes the read target for the advection phase.
    /// </remarks>
    private static void ExecuteSplatPhase(
        FluidSimulationHandle handle,
        List<FluidCommand> commands,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader splatShader = AssetRegistry.GennedShaders.FluidSplat;
        ManagedShader omniShader = AssetRegistry.GennedShaders.FluidOmnidirectional;

        // Step A: Copy the current read target into the write target verbatim.
        // This seeds the write target with the existing field state so that
        // subsequent additive splat draws layer on top rather than onto a blank canvas.
        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);
        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        splatShader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.PointClamp);
        splatShader.Render("PassthroughPass");
        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();

        // Step B: Draw all splat commands additively onto the write target.
        // WriteVelocityDensity is still the active render target from Step A.
        // SpriteSortMode.Immediate allows shader parameters to change between individual draw calls within the same Begin/End block.
        sb.Begin(SpriteSortMode.Immediate, AdditiveBlendNoAlpha, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        foreach (FluidCommand cmd in commands)
        {
            switch (cmd.Type)
            {
                case FluidCommandType.Splat:
                    // The brush texture's alpha channel masks where injection occurs.
                    // The shader outputs (velocity.x, velocity.y, density, 0) and additive blend accumulates it into the existing field.
                    splatShader.SetTexture(cmd.Brush, 0, SamplerState.PointClamp);
                    splatShader.TrySetParameter("injectVelocity", cmd.Velocity);
                    splatShader.TrySetParameter("injectDensity", cmd.Density);
                    splatShader.Render();
                    sb.Draw(
                        cmd.Brush,
                        cmd.Position,
                        cmd.SourceRect,
                        Color.White,
                        cmd.Rotation,
                        cmd.Origin,
                        cmd.Scale,
                        SpriteEffects.None,
                        0f);
                    break;

                case FluidCommandType.OmnidirectionalSplat:
                {
                    // The omnidirectional shader computes a radially outward velocity
                    // for each fragment based on its distance from the center.
                    // It uses SV_Position to determine grid location and does not read from s0; a white pixel is used as a dummy source.
                    Vector2 centerUV = cmd.Position / new Vector2(w, h);
                    Rectangle omniRect = new Rectangle(
                        (int) (cmd.Position.X - cmd.Scale.X * 0.5f),
                        (int) (cmd.Position.Y - cmd.Scale.Y * 0.5f),
                        (int) cmd.Scale.X,
                        (int) cmd.Scale.Y);

                    Texture2D pixel = AssetRegistry.GennedTextures.Pixel;
                    omniShader.SetTexture(pixel, 0, SamplerState.PointClamp);
                    omniShader.TrySetParameter("gridCenterUV", centerUV);
                    omniShader.TrySetParameter("outwardSpeed", cmd.OutwardSpeed);
                    omniShader.TrySetParameter("injectDensity", cmd.Density);
                    omniShader.TrySetParameter("gridSize", new Vector2(w, h));
                    omniShader.Render();
                    sb.Draw(pixel, omniRect, Color.White);
                    break;
                }

                case FluidCommandType.GlobalForce:
                    // Already accumulated into accumulatedForce on the CPU side
                    // no GPU draw is needed here.
                    break;
            }
        }

        sb.End();
    }

    /// <summary>
    ///     Semi-Lagrangian advection of the velocity and density field.
    ///     Applies the accumulated global force and dissipation decay in the same pass.
    /// </summary>
    private static void ExecuteAdvect(
        FluidSimulationHandle handle,
        Vector2 accumulatedForce,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidAdvect;

        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.LinearClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("globalForce", accumulatedForce);
        shader.TrySetParameter("dissipationDecayFactor", handle.Settings.DissipationDecayFactor);
        shader.TrySetParameter("maxVelocity", MaxVelocity);
        shader.TrySetParameter("delta", handle.Delta);
        shader.Render();

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    private static void ExecuteDiffuse(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidDiffuse;

        // Horizontal pass: read -> write, then swap.
        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);
        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);
        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.LinearClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("diffusionRadius", handle.Settings.DiffusionCoefficient * 15f);
        shader.Render("HorizontalPass");
        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
        handle.SwapVelocityPingPong();

        // Vertical pass: read -> write, then swap.
        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);
        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);
        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.LinearClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("diffusionRadius", handle.Settings.DiffusionCoefficient * 15f);
        shader.Render("VerticalPass");
        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
        handle.SwapVelocityPingPong();
    }

    /// <summary>
    ///     Computes the scalar curl magnitude of the velocity field and writes it
    ///     into <see cref="FluidSimulationHandle.CurlTarget"/>.
    /// </summary>
    private static void ExecuteComputeCurl(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidVorticity;

        gd.SetRenderTarget(handle.CurlTarget);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.LinearClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.Render("ComputeCurlPass");

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    /// <summary>
    ///     Reads the curl magnitude from <see cref="FluidSimulationHandle.CurlTarget"/>
    ///     and the current velocity from the read field, then writes the
    ///     vorticity-confined velocity into the write field.
    /// </summary>
    private static void ExecuteApplyConfinement(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidVorticity;

        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.LinearClamp);
        shader.SetTexture(handle.CurlTarget, 1, SamplerState.LinearClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("vorticity", handle.Settings.Vorticity);
        shader.TrySetParameter("maxVelocity", MaxVelocity);
        shader.Render("ApplyConfinementPass");

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    /// <summary>
    ///     Computes the divergence of the velocity field using central differences
    ///     and writes it into <see cref="FluidSimulationHandle.DivergenceTarget"/>.
    ///     This target is then read by every Jacobi iteration.
    /// </summary>
    private static void ExecuteDivergence(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidDivergence;

        gd.SetRenderTarget(handle.DivergenceTarget);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.PointClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.Render();

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    /// <summary>
    ///     Performs one Jacobi relaxation step of the pressure Poisson solve.
    ///     Reads <see cref="FluidSimulationHandle.ReadPressure"/> and <see cref="FluidSimulationHandle.DivergenceTarget"/>, writes one refined pressure estimate into <see cref="FluidSimulationHandle.WritePressure"/>.
    /// </summary>
    private static void ExecuteJacobi(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidJacobi;

        gd.SetRenderTarget(handle.WritePressure);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadPressure, 0, SamplerState.PointClamp);
        shader.SetTexture(handle.DivergenceTarget, 1, SamplerState.PointClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.Render();

        sb.Draw(handle.ReadPressure, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    /// <summary>
    ///     Subtracts the pressure gradient from velocity to produce a
    ///     divergence-free field. Density is carried through unchanged.
    /// </summary>
    private static void ExecuteProject(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidProject;

        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.PointClamp);
        shader.SetTexture(handle.ReadPressure, 1, SamplerState.PointClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("maxVelocity", MaxVelocity);
        shader.Render();

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    /// <summary>
    ///     Zeros velocity at grid texels whose corresponding world-space position falls within a solid tile.
    ///     Density at those positions is also cleared to reinforce containment.
    /// </summary>
    private static void ExecuteBoundary(
        FluidSimulationHandle handle,
        GraphicsDevice gd,
        SpriteBatch sb)
    {
        int w = handle.Settings.GridWidth;
        int h = handle.Settings.GridHeight;
        ManagedShader shader = AssetRegistry.GennedShaders.FluidBoundary;

        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, shader.Effect, Matrix.Identity);

        // tileTarget is a screen-sized UNORM target populated before projectile
        // drawing each frame. We map simulation grid texels to screen UVs by
        // transforming through world space.
        shader.SetTexture(handle.ReadVelocityDensity, 0, SamplerState.PointClamp);
        shader.SetTexture(TileTargetManager.SolidTilesTarget, 1, SamplerState.PointClamp);
        shader.TrySetParameter("gridSize", new Vector2(w, h));
        shader.TrySetParameter("simulationCenter", handle.Center);
        shader.TrySetParameter("simulationWorldSize", handle.GridSize * handle.Scale);
        shader.TrySetParameter("screenPosition", Main.screenPosition);
        shader.TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
        shader.Render();

        sb.Draw(handle.ReadVelocityDensity, new Rectangle(0, 0, w, h), Color.White);
        sb.End();
    }

    #endregion

    #region Utility

    /// <summary>
    ///     Clears every render target owned by the handle to transparent black.
    ///     Called once when a handle is first leased to erase any residual data from a previous owner.
    /// </summary>
    private static void ClearAllTargets(FluidSimulationHandle handle, GraphicsDevice gd)
    {
        gd.SetRenderTarget(handle.ReadVelocityDensity);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.WriteVelocityDensity);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.ReadPressure);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.WritePressure);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.DivergenceTarget);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.CurlTarget);
        gd.Clear(Color.Transparent);
    }

    /// <summary>
    ///     Clears both pressure targets to zero before each Jacobi solve.
    ///     Starting from zero pressure each frame avoids accumulated drift
    ///     across frames and is correct because the Jacobi solve re-converges
    ///     within its iteration budget every frame.
    /// </summary>
    private static void ClearPressureTargets(FluidSimulationHandle handle, GraphicsDevice gd)
    {
        gd.SetRenderTarget(handle.ReadPressure);
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(handle.WritePressure);
        gd.Clear(Color.Transparent);
    }

    #endregion
}

#endregion

public sealed class TileTargetManager : ModSystem
{
    internal static RenderTarget2D SolidTilesTarget;

    public override void OnModLoad()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent += CaptureTiles;

        if (Main.dedServ)
            return;
        Main.QueueMainThreadAction(() =>
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SolidTilesTarget = new RenderTarget2D(gd, Main.screenWidth, Main.screenHeight, false,
                gd.PresentationParameters.BackBufferFormat, DepthFormat.None);
        });
    }

    public override void OnModUnload()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent -= CaptureTiles;

        Main.QueueMainThreadAction(() =>
        {
            SolidTilesTarget?.Dispose();
            SolidTilesTarget = null;
        });
    }

    private void CaptureTiles()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.SetRenderTarget(SolidTilesTarget);
        gd.Clear(Color.Transparent);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        Main.instance.DrawBlack();
        Vector2 delta = Main.screenPosition - Main.screenLastPosition;
        Main.spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition - delta, Color.White);
        Main.spriteBatch.End();

        gd.SetRenderTarget(null);
    }
}
