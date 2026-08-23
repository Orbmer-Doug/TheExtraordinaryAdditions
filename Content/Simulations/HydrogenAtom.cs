using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Utilities;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace TheExtraordinaryAdditions.Content.Simulations;

// Equations adapted from: kavan010
// Source: https://github.com/kavan010/Atoms/blob/main/src/atom_realtime.cpp

#region Definitions

/// <summary>
/// Defines a single particle.
/// </summary>
/// <param name="pos">The position of this particle</param>
/// <param name="col">The color of this particle</param>
public struct Particle(Vector3 pos, Color col)
{
    public Vector3 Position = pos;
    public Vector3 Velocity;
    public Color Color = col;
}

/// <summary>
/// Stores precomputed CDFs for the current quantum state (n, l, m).
/// Rebuilt only when quantum numbers change.
/// </summary>
public struct QuantumCdfCache
{
    public int N, L, M;
    public float[] RadialCdf;
    public float[] PolarCdf;
    public float RadialStep;
    public float Normalization; // cached for color evaluation too
    public bool IsValid;
}

#endregion

/// <summary>
/// Simulates a hydrogen atom, since it's the only atom (right now) that has exact analytical solutions. <br />
/// This is just the electron probability cloud, so some of these following features are technically missing:
/// <list type="bullet">
/// <item>Emissions Spectrum Colors</item>
/// <item>Spin Quantum Number</item>
/// <item>Energy Levels and Transitions</item>
/// <item>Zeeman Effect</item>
/// </list>
/// </summary>
///
/// <br />
/// 
/// Some resources if you want to go in more for whatever reason:
/// <list type="bullet">
/// <item> https://dlmf.nist.gov/14 - Legendre </item>
/// <item> https://dlmf.nist.gov/18.5 - Laguerre </item>
/// <item> http://hyperphysics.phy-astr.gsu.edu/hbase/quantum/hydcn.html#c1 </item>
/// <item> https://en.wikipedia.org/wiki/Legendre_polynomials </item>
/// <item> https://en.wikipedia.org/wiki/Laguerre_polynomials </item>
/// <item> https://en.wikipedia.org/wiki/Spherical_harmonics </item>
/// <item> https://en.wikipedia.org/wiki/Hydrogen_atom </item>
/// <item> https://eclass.uoa.gr/modules/document/file.php/CHEM248/Griffiths%20-%20Introduction%20to%20Quantum%20Mechanics%203rd%20ed%202018.pdf </item>
/// </list>
public sealed class AtomicParticleSimulation : ModSystem
{
    #region Loading

    public override void Load()
    {
        _activeCount = 0;
        Array.Clear(Particles, 0, Particles.Length);
        Array.Clear(PresenceMask, 0, PresenceMask.Length);

        if (Main.dedServ)
            return;
        Main.QueueMainThreadAction(static () =>
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;

            // Unit sphere geometry, built once
            VertexPositionColor3D[] sphereVerts = new VertexPositionColor3D[VerticesPerSphere];
            for (int i = 0; i <= SphereStacks; i++)
            {
                float stackAngle = MathHelper.PiOver2 - MathHelper.Pi * i / SphereStacks;
                float cosStack = MathF.Cos(stackAngle);
                float sinStack = MathF.Sin(stackAngle);
                for (int j = 0; j <= SphereSectors; j++)
                {
                    float sectorAngle = MathHelper.TwoPi * j / SphereSectors;
                    Vector3 normal = new(
                        cosStack * MathF.Cos(sectorAngle),
                        sinStack,
                        cosStack * MathF.Sin(sectorAngle)
                    );

                    // Store the scaled normal as position
                    // Instance offset is added in shader
                    sphereVerts[i * (SphereSectors + 1) + j] =
                        new VertexPositionColor3D(normal * ElectronRadius, Color.White);
                }
            }

            SphereVertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColor3D),
                VerticesPerSphere, BufferUsage.WriteOnly);
            SphereVertexBuffer.SetData(sphereVerts);

            // Single sphere index buffer
            uint[] indices = new uint[IndicesPerSphere];
            int indiceIndex = 0;
            for (int i = 0; i < SphereStacks; i++)
            {
                for (int j = 0; j < SphereSectors; j++)
                {
                    uint topLeft = (uint) (i * (SphereSectors + 1) + j);
                    uint topRight = (uint) (i * (SphereSectors + 1) + j + 1);
                    uint bottomLeft = (uint) ((i + 1) * (SphereSectors + 1) + j);
                    uint bottomRight = (uint) ((i + 1) * (SphereSectors + 1) + j + 1);

                    indices[indiceIndex++] = topLeft;
                    indices[indiceIndex++] = topRight;
                    indices[indiceIndex++] = bottomLeft;

                    indices[indiceIndex++] = bottomLeft;
                    indices[indiceIndex++] = topRight;
                    indices[indiceIndex++] = bottomRight;
                }
            }

            SphereIndexBuffer = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits,
                IndicesPerSphere, BufferUsage.WriteOnly);
            SphereIndexBuffer.SetData(indices);

            InstanceBuffer = new DynamicVertexBuffer(gd, InstancePositionColor3D.Declaration,
                MaxParticles, BufferUsage.WriteOnly);

            const int protonStacks = 16;
            const int protonSectors = 16;
            const int protonVertexCount = (protonStacks + 1) * (protonSectors + 1);
            const int protonIndexCount = protonStacks * protonSectors * 6;

            VertexPositionColor3D[] protonVerts = new VertexPositionColor3D[protonVertexCount];
            for (int i = 0; i <= protonStacks; i++)
            {
                float stackAngle = MathHelper.PiOver2 - MathHelper.Pi * i / protonStacks;
                float cosStack = MathF.Cos(stackAngle);
                float sinStack = MathF.Sin(stackAngle);
                for (int j = 0; j <= protonSectors; j++)
                {
                    float sectorAngle = MathHelper.TwoPi * j / protonSectors;
                    Vector3 normal = new(
                        cosStack * MathF.Cos(sectorAngle),
                        sinStack,
                        cosStack * MathF.Sin(sectorAngle)
                    );
                    // Warm white-yellow color, distinctly different from electron fire palette
                    protonVerts[i * (protonSectors + 1) + j] =
                        new VertexPositionColor3D(normal * ProtonRadius, new Color(255, 220, 150));
                }
            }

            ProtonVertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColor3D),
                protonVertexCount, BufferUsage.WriteOnly);
            ProtonVertexBuffer.SetData(protonVerts);

            uint[] protonIndices = new uint[protonIndexCount];
            int pIdx = 0;
            for (int i = 0; i < protonStacks; i++)
            {
                for (int j = 0; j < protonSectors; j++)
                {
                    uint tl = (uint) (i * (protonSectors + 1) + j);
                    uint tr = (uint) (i * (protonSectors + 1) + j + 1);
                    uint bl = (uint) ((i + 1) * (protonSectors + 1) + j);
                    uint br = (uint) ((i + 1) * (protonSectors + 1) + j + 1);
                    protonIndices[pIdx++] = tl;
                    protonIndices[pIdx++] = tr;
                    protonIndices[pIdx++] = bl;
                    protonIndices[pIdx++] = bl;
                    protonIndices[pIdx++] = tr;
                    protonIndices[pIdx++] = br;
                }
            }

            ProtonIndexBuffer = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits,
                protonIndexCount, BufferUsage.WriteOnly);
            ProtonIndexBuffer.SetData(protonIndices);

            On_Main.DrawDust += RenderParticles;
        });
    }

    public override void Unload()
    {
        // Clean up buffers
        _activeCount = 0;
        Array.Clear(Particles, 0, Particles.Length);
        Array.Clear(PresenceMask, 0, PresenceMask.Length);

        Main.QueueMainThreadAction(static () =>
        {
            SphereVertexBuffer?.Dispose();
            SphereVertexBuffer = null;
            SphereIndexBuffer?.Dispose();
            SphereIndexBuffer = null;
            InstanceBuffer?.Dispose();
            InstanceBuffer = null;

            ProtonVertexBuffer?.Dispose();
            ProtonVertexBuffer = null;
            ProtonIndexBuffer?.Dispose();
            ProtonIndexBuffer = null;

            On_Main.DrawDust -= RenderParticles;
        });
    }

    #endregion

    #region Quantum Constants

    // Using Hartree atomic units here (all set to 1) so things are actually legible

    /// <summary>
    /// a0, the most probable distance between the proton and electron in a ground state hydrogen atom.
    /// </summary>
    public const float BohrRadius = 1f; // 5.29e-11f;

    /// <summary>
    /// The scale factor for how fast the electron circulates.
    /// </summary>
    public const float PlancksConstant = 1f; // 6.62607015e-34f / MathHelper.TwoPi;

    /// <summary>
    /// The invariant mass of an electron.
    /// </summary>
    public const float ElectronMass = 1f; // 9.109e-31f;

    #endregion

    #region Orbital State Quantum Numbers

    /// <summary>
    /// <i>n</i>, controls the energy level and overall size of the orbital. <br />
    /// This is what people mean by "electron shell". <br />
    /// Higher numbers means the electron is more likely to be found further from the nucleus and has higher energy.
    /// </summary>
    internal static int Principal = 3;

    /// <summary>
    /// <i>l</i>, the angular momentum. <br />
    /// Controls the shape of the orbital. <br />
    /// 0 = sphere (s), 1 = dumbbell (p), 2 = clover (d), 3 = complex (f), and so on.
    /// </summary>
    /// <remarks>Must be between 0 and <see cref="Principal"/> - 1</remarks>
    internal static int Azimuthal = 0;

    /// <summary>
    /// <i>m</i>, controls the orientation of the orbital in space. For a given l there are 2l+l possible orientations,
    /// which is why p orbitals come in sets of three (px, py, pz) and d orbitals in sets of five. <br />
    /// In the abscence of a magnetic field all same-l orbitals have identical energy, but a magnetic field breaks that degeneracy and splits them.
    /// </summary>
    internal static int Magnetic = 1;

    #endregion

    #region Render States

    /// <summary>
    /// Additive instead if false.
    /// </summary>
    public static bool UsePreMultiplied;

    /// <summary>
    /// If the <see cref="InstancePool"/> should be sorted by depth.
    /// </summary>
    public static bool SortByDistance;

    #endregion

    #region Evaluation and Calculation

    /// <summary>
    /// Evaluates the associated Laguerre polynomial L_k^alpha(rho) using the
    /// three-term recurrence relation, where:
    /// <list type="">
    /// <item>k     = n - l - 1  (degree, equals the number of radial nodes)</item>
    /// <item>alpha = 2l + 1     (order)</item>
    /// <item>rho   = 2r / (n*a0) (dimensionless scaled radius)</item>
    /// </list>
    /// 
    /// <br />
    ///
    /// Recurrence (Rodrigues' formula derived):
    /// <list type="">
    /// <item> L_0^alpha(rho) = 1</item>
    /// <item> L_1^alpha(rho) = 1 + alpha - rho</item>
    /// <item>  L_j^alpha(rho) = ((2j - 1 + alpha - rho) * L_{j-1} - (j - 1 + alpha) * L_{j-2}) / j </item>
    /// </list>
    /// 
    /// <br />
    /// 
    /// This is numerically stable for the small degrees encountered in low-n orbitals.
    /// </summary>
    /// <param name="degree">k = n - l - 1, the number of radial nodes</param>
    /// <param name="order">alpha = 2l + 1</param>
    /// <param name="scaledRadius">rho = 2r / (n * a0)</param>
    public static float EvaluateLaguerre(int degree, int order, float scaledRadius)
    {
        // L_0 = 1 (constant polynomial, covers s-type orbitals where k=0)
        if (degree == 0)
            return 1f;

        // L_1 = 1 + alpha - rho
        float laguerreCurrent = 1f + order - scaledRadius;
        if (degree == 1)
            return laguerreCurrent;

        // General case: recur from L_1 up to L_degree
        float laguerrePrev = 1f;
        for (int j = 2; j <= degree; ++j)
        {
            float laguerreNext = ((2 * j - 1 + order - scaledRadius) * laguerreCurrent
                                  - (j - 1 + order) * laguerrePrev) / j;
            laguerrePrev = laguerreCurrent;
            laguerreCurrent = laguerreNext;
        }

        return laguerreCurrent;
    }

    /// <summary>
    /// Evaluates the associated Legendre polynomial P_l^m(x) where x = cos(theta),
    /// using upward recurrence in l starting from the closed-form seed P_m^m.
    ///
    /// <br />
    /// <br />
    /// 
    /// Only handles m >= 0. Negative m is related by:
    /// P_l^{-m}(x) = (-1)^m * (l-m)! / (l+m)! * P_l^m(x)
    /// <br />
    /// but since |Y_l^m|^2 = |Y_l^{-m}|^2, negative m produces identical orbital shapes.
    /// 
    /// <br />
    /// 
    /// Seed (closed form):
    /// <list type="">
    /// <item> P_m^m(x) = (-1)^m * (2m-1)!! * (1 - x^2)^(m/2) </item>
    /// <item> where (2m-1)!! = 1 * 3 * 5 * ... * (2m-1) is the double factorial </item>
    /// </list>
    /// 
    /// <br />
    /// 
    /// Recurrence:
    /// <list type="">
    /// <item> P_{m+1}^m(x) = x * (2m + 1) * P_m^m(x) </item>
    /// <item> P_l^m(x) = ((2l - 1) * x * P_{l-1}^m - (l + m - 1) * P_{l-2}^m) / (l - m) </item>
    /// </list>
    /// 
    /// </summary>
    /// 
    /// <param name="azimuthal">l, the azimuthal quantum number</param>
    /// <param name="magnetic">m, the magnetic quantum number (must be >= 0)</param>
    /// <param name="cosTheta">x = cos(theta), the argument of the polynomial</param>
    public static float EvaluateLegendre(int azimuthal, int magnetic, float cosTheta)
    {
        // Seed P_m^m via the double factorial closed form
        // Each iteration multiplies by -(2j-1) * sin(theta)
        float sinTheta = MathF.Sqrt(MathF.Max(0f, (1f - cosTheta) * (1f + cosTheta)));
        float legendreDiagonal = 1f; // P_m^m seed before loop
        float doubleFactorial = 1f;
        for (int j = 1; j <= magnetic; ++j)
        {
            legendreDiagonal *= -doubleFactorial * sinTheta;
            doubleFactorial += 2f;
        }

        // P_l^m = P_m^m when l == m (e.g. all d_{m=2} type orbitals at their seed)
        if (azimuthal == magnetic)
            return legendreDiagonal;

        // P_{m+1}^m: one step above the diagonal
        float legendreOneBelowTarget = cosTheta * (2 * magnetic + 1) * legendreDiagonal;
        if (azimuthal == magnetic + 1)
            return legendreOneBelowTarget;

        // General case: recur from P_{m+1}^m up to P_l^m
        float legendreTwoBelowTarget = legendreDiagonal;
        for (int degree = magnetic + 2; degree <= azimuthal; ++degree)
        {
            float legendreNext = ((2f * degree - 1) * cosTheta * legendreOneBelowTarget
                                  - (degree + magnetic - 1) * legendreTwoBelowTarget)
                                 / (degree - magnetic);
            legendreTwoBelowTarget = legendreOneBelowTarget;
            legendreOneBelowTarget = legendreNext;
        }

        return legendreOneBelowTarget;
    }

    /// <summary>
    /// Computes the probability current velocity for a particle at its position.
    ///
    /// <br />
    /// <br />
    /// 
    /// In quantum mechanics, the probability current J describes the flow of probability
    /// density through space. For a hydrogen eigenstate with quantum numbers (n, l, m),
    /// the current has only an azimuthal (phi) component due to the e^(i*m*phi) phase factor
    /// in the wavefunction. This gives rise to orbital angular momentum.
    /// </summary>
    public static Vector3 CalculateProbabilityCurrentVelocity(Particle p, int n, int l, int m)
    {
        float radius = p.Position.Length();
        if (radius < 1e-6)
            return Vector3.Zero;

        // Convert Cartesian position to spherical coordinates
        // theta: polar angle from +Y axis (colatitude), range [0, pi]
        // phi: azimuthal angle in XZ plane from +X axis, range [-pi, pi]
        float polarAngle = MathF.Acos(p.Position.Y / radius);
        float azimuthalAngle = MathF.Atan2(p.Position.Z, p.Position.X);

        // sin(theta) appears in the denominator from the azimuthal component of grad(phi)
        // Clamp away from zero to avoid singularity at the poles (theta = 0 or pi)
        float sinPolar = MathF.Sin(polarAngle);
        const float poleSingularityThreshold = 1e-4f;
        if (MathF.Abs(sinPolar) < poleSingularityThreshold)
            sinPolar = poleSingularityThreshold;

        // Speed of the azimuthal probability flow
        // This is the quantum mechanical analog of angular momentum conservation
        float azimuthalSpeed = PlancksConstant * m / (ElectronMass * radius * sinPolar);

        /* Zeeman Effect?
        float fieldStrength = 5f;
        float larmorSpeed = (fieldStrength / 2f) / (radius * sinPolar);
        azimuthalSpeed += larmorSpeed;
        */

        // Convert azimuthal (phi-hat direction) velocity to Cartesian components
        // phi-hat = (-sin(phi), 0, cos(phi)) in (x, y, z) coordinates
        float velocityX = -azimuthalSpeed * MathF.Sin(azimuthalAngle);
        const float velocityY = 0f; // No polar component for these eigenstates
        float velocityZ = azimuthalSpeed * MathF.Cos(azimuthalAngle);

        return new Vector3(velocityX, velocityY, velocityZ);
    }

    private const float PolarStep = MathHelper.Pi / (PolarCdfGridPoints - 1);
    private const int RadialCdfGridPoints = 4096;
    private const int PolarCdfGridPoints = 2048;
    private static QuantumCdfCache _cdfCache;

    private static float[] BuildRadialCDF(int principal, int azimuthal, out float radialStep, out float normalization)
    {
        // Orbital radius scales as n^2 * a0, so we sample out to 10x that to capture the tail
        float maxRadius = 10f * principal * principal * BohrRadius;
        radialStep = maxRadius / (RadialCdfGridPoints - 1);

        Span<float> cdf = new float[RadialCdfGridPoints];

        // Recurrence indices for the associated Laguerre polynomial L_k^alpha(rho)
        // k = n - l - 1 is the degree (number of radial nodes)
        // alpha = 2l + 1 is the order
        int laguerreDegree = principal - azimuthal - 1;
        int laguerreOrder = 2 * azimuthal + 1;

        // Normalization constant for the radial wavefunction |R_nl|^2
        // Derived from orthonormality of hydrogen eigenstates
        normalization = MathF.Pow(2f / (principal * BohrRadius), 3f)
                        * (float) Gamma(principal - azimuthal)
                        / (2f * principal
                              * (float) Gamma(principal + azimuthal + 1));

        float cdfRunningSum = 0f;
        for (int i = 0; i < RadialCdfGridPoints; ++i)
        {
            float r = i * radialStep;
            float scaledRadius = 2f * r / (principal * BohrRadius); // rho = 2r / (n*a0)

            float laguerreValue = EvaluateLaguerre(laguerreDegree, laguerreOrder, scaledRadius);

            // Radial wavefunction: R_nl(r) = sqrt(norm) * exp(-rho/2) * rho^l * L_k^alpha(rho)
            float radialWavefunction = MathF.Sqrt(normalization)
                                       * MathF.Exp(-scaledRadius / 2f)
                                       * MathF.Pow(scaledRadius, azimuthal)
                                       * laguerreValue;

            // Radial probability density P(r) = r^2 * |R_nl|^2
            // The r^2 factor is the Jacobian of spherical coordinates
            float radialProbabilityDensity = r * r * radialWavefunction * radialWavefunction;

            cdfRunningSum += radialProbabilityDensity;
            cdf[i] = cdfRunningSum;
        }

        // Normalize CDF to [0, 1]
        for (int i = 0; i < RadialCdfGridPoints; i++)
            cdf[i] /= cdfRunningSum;

        return cdf.ToArray();
    }

    private static float[] BuildPolarCDF(int azimuthal, int magnetic)
    {
        Span<float> cdf = new float[PolarCdfGridPoints];
        float cdfRunningSum = 0f;

        for (int i = 0; i < PolarCdfGridPoints; ++i)
        {
            float polarAngle = i * PolarStep;
            float cosTheta = MathF.Cos(polarAngle);

            float associatedLegendre = EvaluateLegendre(azimuthal, magnetic, cosTheta);

            // Angular probability density: sin(theta) * |P_l^m(cos theta)|^2
            float angularProbabilityDensity = MathF.Sin(polarAngle) * associatedLegendre * associatedLegendre;
            cdfRunningSum += angularProbabilityDensity;
            cdf[i] = cdfRunningSum;
        }

        for (int i = 0; i < PolarCdfGridPoints; i++)
            cdf[i] /= cdfRunningSum;

        return cdf.ToArray();
    }

    private static void EnsureCacheValid(int n, int l, int m)
    {
        if (_cdfCache.IsValid && _cdfCache.N == n && _cdfCache.L == l && _cdfCache.M == m)
            return;

        _cdfCache.RadialCdf = BuildRadialCDF(n, l, out _cdfCache.RadialStep, out _cdfCache.Normalization);
        _cdfCache.PolarCdf = BuildPolarCDF(l, m);
        _cdfCache.N = n;
        _cdfCache.L = l;
        _cdfCache.M = m;
        _cdfCache.IsValid = true;
    }

    /// <summary>
    /// Samples a radial distance r from the hydrogen radial probability distribution P(r).
    ///
    /// <br />
    /// <br />
    /// 
    /// The radial wavefunction R_nl(r) is built from:
    /// <list type="bullet">
    /// <item> An exponential decay: exp(-rho/2), where rho = 2r/(n*a0) </item>
    /// <item> A power law: rho^l </item>
    /// <item> An associated Laguerre polynomial: L_k^alpha(rho), k = n-l-1 </item>
    /// </list>
    /// 
    /// The radial probability density is P(r) = r^2 * |R_nl(r)|^2, where the r^2
    /// factor comes from the spherical volume element r^2 sin(theta) dr dtheta dphi.
    ///
    /// <br />
    /// <br />
    /// 
    /// We build a cumulative distribution function (CDF) over a discrete grid,
    /// then invert it with a uniform random sample (inverse transform sampling).
    /// </summary>
    public static float SampleRadialDistance(int principal, int azimuthal)
    {
        ReadOnlySpan<float> cdf = _cdfCache.RadialCdf;

        // Inverse transform sampling: draw uniform u in [0,1], find where CDF crosses it
        float uniformSample = Main.rand.NextFloat();
        int sampledIndex = cdf.BinarySearch(uniformSample);
        if (sampledIndex < 0)
            sampledIndex = ~sampledIndex;
        sampledIndex = Math.Clamp(sampledIndex, 0, RadialCdfGridPoints - 1);

        return sampledIndex * _cdfCache.RadialStep;
    }

    /// <summary>
    /// Samples a polar angle theta from the angular probability distribution P(theta).
    ///
    /// <br />
    /// <br />
    /// 
    /// The angular part of the hydrogen wavefunction is a spherical harmonic Y_l^m(theta, phi).
    /// Its theta-dependent factor is the associated Legendre polynomial P_l^m(cos theta), normalized so that the probability density over the sphere is:
    /// <br />
    /// P(theta) = sin(theta) * |P_l^m(cos theta)|^2
    ///
    /// <br />
    /// <br />
    /// 
    /// The sin(theta) factor is again the spherical coordinate Jacobian.
    /// We use the same inverse-transform CDF approach as SampleRadialDistance.
    ///
    /// <br />
    /// <br />
    /// 
    /// P_l^m is evaluated via upward recurrence from P_m^m:
    /// <list type="">
    /// <item> P_m^m(x) = (-1)^m * (2m-1)!! * (1 - x^2)^(m/2) </item>
    /// <item> P_{m+1}^m(x) = x*(2m+1)*P_m^m </item>
    /// <item> P_l^m(x) = ((2l-1)*x*P_{l-1}^m - (l+m-1)*P_{l-2}^m) / (l-m) </item>
    /// </list>
    /// </summary>
    public static float SamplePolarAngle(int azimuthal, int magnetic)
    {
        ReadOnlySpan<float> cdf = _cdfCache.PolarCdf;

        float uniformSample = Main.rand.NextFloat();
        int sampledIndex = cdf.BinarySearch(uniformSample);
        if (sampledIndex < 0)
            sampledIndex = ~sampledIndex;
        sampledIndex = Math.Clamp(sampledIndex, 0, PolarCdfGridPoints - 1);

        return sampledIndex * PolarStep;
    }

    /// <summary>
    /// Samples an azimuthal angle phi uniformly from [0, 2*pi).
    ///
    /// <br />
    /// <br />
    /// 
    /// The phi-dependent part of |Y_l^m|^2 is |e^(i*m*phi)|^2 = 1, so the probability
    /// distribution over phi is perfectly uniform regardless of m.
    /// </summary>
    public static float SampleAzimuthalAngle()
    {
        return 2.0f * MathHelper.Pi * Main.rand.NextFloat();
    }

    /// <summary>
    /// Computes a color for a particle based on |psi(r, theta, phi)|^2 using
    /// logarithmic compression to handle the enormous dynamic range of the wavefunction.
    ///
    /// <br />
    /// <br />
    /// 
    /// The full probability density is:
    /// <list type="">
    /// <item> |psi|^2 = |R_nl(r)|^2 * |Y_l^m(theta, phi)|^2 </item>
    /// <item> = |R_nl(r)|^2 * |P_l^m(cos theta)|^2 / (2*pi) </item>
    /// </list>
    /// 
    /// <br />
    /// 
    /// Raw intensity spans many orders of magnitude, so we apply log10 compression
    /// before mapping to the inferno colormap. The +1e-12 guard prevents log(0).
    /// </summary>
    public static Vector4 ComputeWavefunctionColorLogarithmic(float r, float theta, int principal, int azimuthal,
        int magnetic)
    {
        float scaledRadius = 2f * r / (principal * BohrRadius);

        int laguerreDegree = principal - azimuthal - 1;
        int laguerreOrder = 2 * azimuthal + 1;
        float laguerreValue = EvaluateLaguerre(laguerreDegree, laguerreOrder, scaledRadius);

        float normalization = MathF.Pow(2f / (principal * BohrRadius), 3)
                              * (float) Gamma(principal - azimuthal)
                              / (2f * principal * (float) Gamma(principal + azimuthal + 1));

        float radialWavefunction = MathF.Sqrt(normalization)
                                   * MathF.Exp(-scaledRadius / 2f)
                                   * MathF.Pow(scaledRadius, azimuthal)
                                   * laguerreValue;

        float radialDensity = radialWavefunction * radialWavefunction;

        // Evaluate P_l^m(cos theta) (same recurrence as SamplePolarAngle)
        float cosTheta = MathF.Cos(theta);
        float associatedLegendre = EvaluateLegendre(azimuthal, magnetic, cosTheta);
        float angularDensity = associatedLegendre * associatedLegendre;

        // Full probability density |psi|^2 = |R_nl|^2 * |P_l^m|^2
        float probabilityDensity = radialDensity * angularDensity;

        // Log10 compress into [0, 1]: intensity near 1e-12 maps to 0, near 1.0 maps to 1
        float compressedIntensity = MathF.Log10(probabilityDensity + 1e-12f) + 12f;
        compressedIntensity /= 12f;
        compressedIntensity = MathHelper.Clamp(compressedIntensity, 0f, 1f);

        // Inferno-style ramp: red channel rises first, then green, then a hint of blue at peak
        float red = MathHelper.SmoothStep(0.15f, 1.0f, compressedIntensity);
        float green = MathHelper.SmoothStep(0.45f, 1.0f, compressedIntensity) * 0.8f;
        float blue = MathHelper.SmoothStep(0.85f, 1.0f, compressedIntensity) * 0.2f;

        return new Vector4(red, green, blue, 1.0f);
    }

    public static readonly Vector4[] Colors =
    [
        new Vector4(0f, 0f, 0f, 1f), // Black
        new Vector4(.3f, 0f, .6f, 1f), // Dark Purple
        new Vector4(.8f, 0f, 0f, 1f), // Deep Red
        new Vector4(1f, .5f, 0f, 1f), // Orange
        new Vector4(1f, 1f, 0f, 1f), // Yellow
        new Vector4(1f, 1f, 1f, 1f) // White
    ];

    /// <summary>
    /// Maps a normalized intensity value in [0, 1] to a fire-themed color
    /// by linearly interpolating between a set of perceptually ordered color stops.
    /// 
    /// <br />
    /// 
    /// Stop order (black body radiation inspired):
    /// <list type="bullet">
    /// <item>  0.0 = Black (no emission) </item>
    /// <item> 0.2 = Dark Purple (faint, cool)</item>
    /// <item> 0.4 = Deep Red (warm)</item>
    /// <item> 0.6 = Orange</item>
    /// <item> 0.8 = Yellow (bright)</item>
    /// <item>  1.0 = White (peak intensity, all channels saturated)</item>
    /// </list>
    /// 
    /// </summary>
    public static Vector4 SampleFireColormap(float normalizedIntensity)
    {
        normalizedIntensity = MathF.Max(0f, MathF.Min(1f, normalizedIntensity));

        // Scale intensity into the color stop array index space
        float scaledIndex = normalizedIntensity * (Colors.Length - 1);
        int lowerStop = (int) scaledIndex;
        int upperStop = int.Min(lowerStop + 1, Colors.Length - 1);

        // Fractional position between the two surrounding stops
        float blend = scaledIndex - lowerStop;

        return new Vector4(
            Colors[lowerStop].X + blend * (Colors[upperStop].X - Colors[lowerStop].X),
            Colors[lowerStop].Y + blend * (Colors[upperStop].Y - Colors[lowerStop].Y),
            Colors[lowerStop].Z + blend * (Colors[upperStop].Z - Colors[lowerStop].Z),
            UsePreMultiplied ? .1f : 1f
        );
    }

    /// <summary>
    /// Computes a display color for a particle at spherical coordinates (r, theta, phi)
    /// based on the hydrogen wavefunction probability density |psi_nlm|^2.
    ///
    /// <br />
    /// <br />
    /// 
    /// Unlike ComputeWavefunctionColorLogarithmic, this uses linear scaling with a
    /// manual LightingScaler constant to map the raw density into [0, 1] before
    /// passing to the fire colormap. Better for visualizing relative density differences
    /// in a single orbital where the dynamic range is manageable.
    ///
    /// <br />
    /// <br />
    ///
    /// Full probability density:
    /// <list type="">
    /// <item> |psi_nlm|^2 = |R_nl(r)|^2 * |P_l^m(cos theta)|^2 </item>
    /// <item> where phi drops out because |e^(i*m*phi)|^2 = 1. </item>
    /// </list>
    /// 
    /// </summary>
    public static Vector4 ComputeWavefunctionColor(float r, float theta, int principal, int azimuthal, int magnetic)
    {
        EnsureCacheValid(principal, azimuthal, magnetic);
        float scaledRadius = 2f * r / (principal * BohrRadius);

        int laguerreDegree = principal - azimuthal - 1;
        int laguerreOrder = 2 * azimuthal + 1;
        float laguerreValue = EvaluateLaguerre(laguerreDegree, laguerreOrder, scaledRadius);

        float radialWavefunction = MathF.Sqrt(_cdfCache.Normalization)
                                   * MathF.Exp(-scaledRadius / 2f)
                                   * MathF.Pow(scaledRadius, azimuthal)
                                   * laguerreValue;

        float radialDensity = radialWavefunction * radialWavefunction;

        // Evaluate P_l^m(cos theta) via upward recurrence
        float cosTheta = MathF.Cos(theta);
        float associatedLegendre = EvaluateLegendre(azimuthal, magnetic, cosTheta);
        float angularDensity = associatedLegendre * associatedLegendre;

        // Scale raw density linearly into colormap range
        float probabilityDensity = radialDensity * angularDensity;
        return SampleFireColormap(probabilityDensity * 1.5f * MathF.Pow(5, principal));
    }

    /// <summary>
    /// Returns a Gaussian-distributed random sample with the given mean and standard deviation,
    /// using the Box-Muller transform on two uniform samples U1, U2 in (0, 1):
    ///   X = sqrt(-2 * ln(U2)) * cos(2*pi * U1)
    ///   Y = sqrt(-2 * ln(U2)) * sin(2*pi * U1)
    /// Both X and Y are standard normal
    /// </summary>
    private static float GaussianSample(float mean, float stdDev)
    {
        // Draw two uniform samples, guarding against log(0)
        float u1 = MathF.Max(Main.rand.NextFloat(), 1e-7f);
        float u2 = MathF.Max(Main.rand.NextFloat(), 1e-7f);

        float radius = MathF.Sqrt(-2f * MathF.Log(u2));
        float angle = MathHelper.TwoPi * u1;

        return mean + stdDev * (radius * MathF.Cos(angle)); // only x
    }

    #endregion

    #region Updates and Rendering

    private static readonly Particle[] Particles = new Particle[MaxParticles];
    private static readonly ulong[] PresenceMask = BitmaskUtils.CreateMask(MaxParticles);
    private static int _activeCount;

    public static BitmaskUtils.IndicesEnumerable ActiveParticles =>
        new BitmaskUtils.IndicesEnumerable(PresenceMask.AsSpan(0, PresenceMask.Length), MaxParticles);

    /// <summary>
    /// The maximum amount of particles allowed in the simulation.
    /// </summary>
    // Note: After around ~250,000 is when some frames start stuttering, but otherwise still clean
    public const int MaxParticles = 1_000_000;

    /// <summary>
    /// The maximum size of every electron.
    /// </summary>
    public const float ElectronRadius = .25f;

    /// <summary>
    /// The maximum side of the proton.
    /// </summary>
    public const float ProtonRadius = 1.2f;

    internal static int CycleIndex;

    public static void UpdateKeys()
    {
        if (Keys.L.JustPressed())
        {
            CycleIndex = (CycleIndex + 1) % 3;
            DisplayCurrentValue(true);
        }
        else if (Keys.J.JustPressed())
        {
            CycleIndex -= 1;
            if (CycleIndex < 0)
                CycleIndex = 2;
            DisplayCurrentValue(true);
        }

        if (Keys.O.JustPressed())
        {
            UsePreMultiplied = !UsePreMultiplied;
            DirectlyDisplayText($"{(UsePreMultiplied ? "Using Pre-Multiplied" : "Using Additive")}");
        }

        if (Keys.U.JustPressed())
        {
            SortByDistance = !SortByDistance;
            DirectlyDisplayText($"Sorting particles? {SortByDistance}");
        }

        if (Keys.K.JustPressed())
        {
            switch (CycleIndex)
            {
                case 0:
                    if (Principal > 1)
                        Principal--;
                    break;
                case 1:
                    if (Azimuthal > 0)
                        Azimuthal--;
                    break;
                case 2:
                    if (Magnetic > 0)
                        Magnetic--;
                    break;
            }

            DisplayCurrentValue(false);
        }
        else if (Keys.I.JustPressed())
        {
            switch (CycleIndex)
            {
                case 0:
                    Principal++;
                    break;
                case 1:
                    Azimuthal++;
                    break;
                case 2:
                    Magnetic++;
                    break;
            }

            DisplayCurrentValue(false);
        }

        return;

        /*
        if (Azimuthal >= Principal)
            Azimuthal = Principal - 1;
        if (Magnetic > Azimuthal)
            Magnetic = Azimuthal;
        */
        void DisplayCurrentValue(bool change)
        {
            string add = change ? "on:" : string.Empty;
            switch (CycleIndex)
            {
                case 0:
                    DirectlyDisplayText($"{add} principal = {Principal}");
                    break;
                case 1:
                    DirectlyDisplayText($"{add} azimuthal = {Azimuthal}");
                    break;
                case 2:
                    DirectlyDisplayText($"{add} magnetic = {Magnetic}");
                    break;
            }
        }
    }

    public static void Add(Particle particle)
    {
        if (_activeCount >= MaxParticles || Main.gamePaused || Main.dedServ)
            return;

        int index = BitmaskUtils.AllocateIndex(PresenceMask, MaxParticles);
        Particles[index] = particle;
        Interlocked.Increment(ref _activeCount);
    }

    public static void MakeParticles(Vector3 position, int num)
    {
        EnsureCacheValid(Principal, Azimuthal, Magnetic);

        for (int i = 0; i < num; i++)
        {
            Vector3 pos = position + SphericalToCartesian(
                SampleRadialDistance(Principal, Azimuthal),
                SamplePolarAngle(Azimuthal, Magnetic),
                SampleAzimuthalAngle()
            );
            float r = pos.Length();
            float theta = MathF.Acos(pos.Y / r);
            Vector4 col = ComputeWavefunctionColor(r, theta, Principal, Azimuthal, Magnetic);

            Add(new Particle(pos, new(col)));
        }
    }

    internal static Camera Camera = new();

    internal static VertexBuffer SphereVertexBuffer; // static, one sphere
    internal static IndexBuffer SphereIndexBuffer; // static, one sphere
    internal static DynamicVertexBuffer InstanceBuffer; // dynamic, one entry per particle

    internal static VertexBuffer ProtonVertexBuffer;
    internal static IndexBuffer ProtonIndexBuffer;

    private static readonly InstancePositionColor3D[] InstancePool = new InstancePositionColor3D[MaxParticles];

    internal const int SphereStacks = 6;
    internal const int SphereSectors = 6;

    // +1 for first and last vertex in each ring
    internal const int VerticesPerSphere = (SphereStacks + 1) * (SphereSectors + 1);

    // 2 triangles per quad = 6 indices
    internal const int IndicesPerSphere = SphereStacks * SphereSectors * 6;

    // parallelize...?
    private const int DepthBuckets = 24;

    private static readonly List<InstancePositionColor3D>[] Buckets =
        Enumerable.Range(0, DepthBuckets).Select(_ => new List<InstancePositionColor3D>()).ToArray();

    private static void BucketSortInstances(int count, Vector3 cameraPos)
    {
        float maxDist = 0f;
        for (int i = 0; i < count; i++)
            maxDist = MathF.Max(maxDist,
                Vector3.DistanceSquared(InstancePool[i].Position, cameraPos));

        foreach (List<InstancePositionColor3D> b in Buckets)
            b.Clear();

        // Assign each instance to a depth bucket
        for (int i = 0; i < count; i++)
        {
            float dist = Vector3.DistanceSquared(InstancePool[i].Position, cameraPos);
            int bucket = (int) (dist / maxDist * (DepthBuckets - 1));
            Buckets[bucket].Add(InstancePool[i]);
        }

        // Write back far-to-near
        int writeIndex = 0;
        for (int b = DepthBuckets - 1; b >= 0; b--)
            foreach (InstancePositionColor3D instance in Buckets[b])
                InstancePool[writeIndex++] = instance;
    }

    public static void RenderParticles(On_Main.orig_DrawDust orig, Main self)
    {
        if (!EnableSimulation.Enabled)
        {
            orig(self);
            return;
        }
        
        int width = Main.screenWidth;
        int height = Main.screenHeight;

        Matrix view = Matrix.CreateLookAt(Camera.Position(), Camera.Target, Vector3.Up);
        Matrix proj = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45f), (float) width / height, .1f, 10_000f);

        ManagedShader shader = AssetRegistry.GennedShaders.ElectronShader;
        shader.TrySetParameter("model", Matrix.Identity);
        shader.TrySetParameter("view", view);
        shader.TrySetParameter("projection", proj);
        shader.Render();

        GraphicsDevice gd = Main.graphics.GraphicsDevice;

        // Build compact instance array by iterating live slots
        int instanceCount = 0;
        foreach (int index in ActiveParticles)
        {
            InstancePool[instanceCount++] = new InstancePositionColor3D(
                Particles[index].Position,
                Particles[index].Color
            );
        }

        if (instanceCount == 0)
        {
            orig(self);
            return;
        }

        if (SortByDistance)
            BucketSortInstances(instanceCount, Camera.Position());

        // Upload only the live instances
        InstanceBuffer.SetData(InstancePool, 0, instanceCount);

        // Bind sphere geometry + instance data as two streams
        gd.SetVertexBuffers(
            new VertexBufferBinding(SphereVertexBuffer, 0, 0), // freq 0 = per vertex
            new VertexBufferBinding(InstanceBuffer, 0, 1) // freq 1 = per instance
        );
        gd.Indices = SphereIndexBuffer;

        BlendState prevBlend = gd.BlendState;
        RasterizerState prevRast = gd.RasterizerState;
        DepthStencilState prevDepth = gd.DepthStencilState;
        gd.BlendState = UsePreMultiplied ? BlendState.NonPremultiplied : BlendState.Additive;
        gd.RasterizerState = RasterizerState.CullCounterClockwise;
        gd.DepthStencilState = DepthStencilState.DepthRead;

        gd.DrawInstancedPrimitives(
            PrimitiveType.TriangleList,
            0, // baseVertex
            0, // minVertexIndex
            VerticesPerSphere, // numVertices (one sphere)
            0, // startIndex
            IndicesPerSphere / 3, // primitiveCount (one sphere)
            instanceCount // how many instances to draw
        );

        // Draw proton
        /*
        gd.SetVertexBuffer(ProtonVertexBuffer);
        gd.Indices = ProtonIndexBuffer;

        shader.Render("SingleObject");
        gd.DrawIndexedPrimitives(
            PrimitiveType.TriangleList,
            0, 0,
            ProtonVertexBuffer.VertexCount,
            0,
            ProtonIndexBuffer.IndexCount / 3
        );
        */

        gd.BlendState = prevBlend;
        gd.RasterizerState = prevRast;
        gd.DepthStencilState = prevDepth;

        gd.SetVertexBuffer(null);
        gd.SetVertexBuffers(null);
        gd.Indices = null;

        orig(self);
    }

    public override void PostUpdateDusts()
    {
        if (Main.gamePaused || !EnableSimulation.Enabled)
            return;

        Camera.ProcessMouseMove(Main.MouseScreen.X, Main.MouseScreen.Y);
        Camera.ProcessScroll(PlayerInput.ScrollWheelDeltaForUI / 120f);
        Camera.ProcessMouseButton();
        Camera.Update();
        UpdateKeys();

        if (Main.keyState.IsKeyDown(Keys.OemCloseBrackets) && _activeCount < MaxParticles)
        {
            MakeParticles(new Vector3(Vector2.Zero, 0f), 10000);
            DirectlyDisplayText($"making {_activeCount}");
        }

        Parallel.For(0, PresenceMask.Length, maskIndex =>
        {
            ref ulong maskRef = ref PresenceMask[maskIndex];
            ulong maskCopy = maskRef;
            int baseIndex = maskIndex * BitmaskUtils.BitsPerMask;

            while (maskCopy != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(maskCopy);
                maskCopy &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                ref Particle p = ref Particles[index];

                float radius = p.Position.Length();
                if (radius > 1e-6)
                {
                    float theta = MathF.Acos(p.Position.Y / radius);
                    p.Velocity = CalculateProbabilityCurrentVelocity(p, Principal, Azimuthal, Magnetic);

                    /*
                    // Applying the uncertainty principle to velocity
                    // Technically yes it's already there but this is just to make it more noticeable
                    float scaledRadius = 2f * radius / (Principal * BohrRadius);
                    int laguerreDegree = Principal - Azimuthal - 1;
                    int laguerreOrder = 2 * Azimuthal + 1;
                    float laguerre = EvaluateLaguerre(laguerreDegree, laguerreOrder, scaledRadius);

                    float radialWave = MathF.Sqrt(_cdfCache.Normalization)
                                       * MathF.Exp(-scaledRadius / 2f)
                                       * MathF.Pow(scaledRadius, Azimuthal)
                                       * laguerre;

                    float probabilityDensity = radius * radius * radialWave * radialWave;
                    float deltaX = 1f / (probabilityDensity + 1e-6f);
                    // Heisenberg says that deltax * deltap >= hbar/2
                    float deltaP = PlancksConstant / (2f * deltaX);
                    deltaP *= 5f; // jitter a little more

                    // Spherical basis vectors at this particle's position
                    float phi = MathF.Atan2(p.Position.Z, p.Position.X);
                    float sinTheta = MathF.Sin(theta);
                    float cosTheta = MathF.Cos(theta);
                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    // r-hat: points away from nucleus, uncertainty from radial wavefunction
                    Vector3 radialHat = new Vector3(
                        sinTheta * cosPhi,
                        cosTheta,
                        sinTheta * sinPhi
                    );

                    // theta-hat: polar tangent, uncertainty from Legendre / angular wavefunction
                    Vector3 polarHat = new Vector3(
                        cosTheta * cosPhi,
                        -sinTheta,
                        cosTheta * sinPhi
                    );

                    // phi-hat: azimuthal tangent, already used in probability current
                    Vector3 azimuthalHat = new Vector3(-sinPhi, 0f, cosPhi);

                    float cosT = MathF.Cos(theta);
                    float legendre = EvaluateLegendre(Azimuthal, Magnetic, cosT);
                    float angularDensity = legendre * legendre;
                    float deltaTheta = 1f / (angularDensity + 1e-6f);
                    float deltaPTheta = PlancksConstant / (2f * deltaTheta);

                    // Azimuthal is uniform in |psi|^2 so just use a fraction of the radial kick
                    float deltaPPhi = deltaP * 0.5f;

                    Vector3 kick = (radialHat * GaussianSample(0f, deltaP)
                                    + polarHat * GaussianSample(0f, deltaPTheta)
                                    + azimuthalHat * GaussianSample(0f, deltaPPhi))
                                   / ElectronMass;
                    p.Velocity += kick;
                    */

                    Vector3 tempPos = p.Position + p.Velocity;
                    float newPhi = MathF.Atan2(tempPos.Z, tempPos.X);
                    p.Position = SphericalToCartesian(radius, theta, newPhi);
                }

                if (!Main.keyState.IsKeyDown(Keys.OemOpenBrackets))
                    continue;
                Interlocked.And(ref maskRef, ~(1ul << bitIndex));
                Interlocked.Decrement(ref _activeCount);
            }
        });
    }

    #endregion
}

internal sealed class EnableSimulation : ModCommand
{
    private static LocalizedText DescText { get; set; }
    private static LocalizedText UsageText { get; set; }

    public override void SetStaticDefaults()
    {
        const string key = "Misc.Atom.";
        DescText = Mod.GetLocalization($"{key}Description");
        UsageText = Mod.GetLocalization($"{key}Usage");
    }

    public static bool Enabled { get; private set; }
    public override CommandType Type => CommandType.Chat;
    public override bool IsCaseSensitive => false;
    public override string Command => "enableatom";
    public override string Description => DescText.Value;
    public override string Usage => UsageText.Value;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Enabled = !Enabled;
        DirectlyDisplayText($"Atomic sim enabled? {Enabled}");
    }
}
