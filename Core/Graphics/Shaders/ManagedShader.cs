using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TheExtraordinaryAdditions.Core.Graphics.Shaders;

/// <summary>
/// The managed effect
/// </summary>
public sealed class ManagedShader : IDisposable
{
    /// <summary>
    /// A managed copy of all parameter data
    /// Used to minimize excess SetValue calls, in cases where the value aren't actually being changed
    /// </summary>
    internal readonly Dictionary<string, object> ParameterCache;

    /// <summary>
    /// The identifying name of this shader
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// The shader reference underlying this wrapper
    /// </summary>
    public readonly Ref<Effect> Shader;

    public Effect Effect => Shader.Value;

    /// <summary>
    /// Whether this shader is disposed
    /// </summary>
    public bool Disposed { get; private set; }

    /// <summary>
    /// The standard parameter name prefix for texture sizes
    /// </summary>
    public const string TextureSizeParameterPrefix = "textureSize";

    /// <summary>
    /// The standard pass name when autoloading shaders
    /// </summary>
    public const string DefaultPassName = "AutoloadPass";

    internal ManagedShader(string name, Ref<Effect> shader)
    {
        Shader = shader;
        Name = name;
        ParameterCache = [];
    }

    internal bool ParameterIsCachedAsValue(string parameterName, object value)
    {
        // If the parameter cache has not registered this parameter yet, that means it can't have changed, because there's nothing to compare against
        if (!ParameterCache.TryGetValue(parameterName, out object parameter))
            return false;

        return parameter?.Equals(value) ?? false;
    }

    /// <summary>
    /// Attempts to send parameter data to the GPU for the shader to use
    /// </summary>
    /// <param name="parameterName">The name of the parameter. This must correspond with the parameter name in the shader</param>
    /// <param name="value">The value to supply to the parameter</param>
    public bool TrySetParameter(string parameterName, object value)
    {
        // Shaders do not work on servers
        if (Main.netMode == NetmodeID.Server)
            return false;

        // Check if the parameter even exists
        EffectParameter parameter = Effect.Parameters[parameterName];
        if (parameter is null)
            return false;

        if (ParameterIsCachedAsValue(parameterName, value))
            return false;

        // Store the value in the cache
        ParameterCache[parameterName] = value;

        // There isn't really a good way to sent stuff over to the GPU due to no simple type numbers can be converted to
        // So until something better appears this switch will do
        try
        {
            switch (value)
            {
                case bool b:
                    parameter.SetValue(b);
                    return true;
                case bool[] b2:
                    parameter.SetValue(b2);
                    return true;
                case int i:
                    parameter.SetValue(i);
                    return true;
                case int[] i2:
                    parameter.SetValue(i2);
                    return true;
                case float f:
                    parameter.SetValue(f);
                    return true;
                case float[] f2:
                    parameter.SetValue(f2);
                    return true;
                case Vector2 v2:
                    parameter.SetValue(v2);
                    return true;
                case Vector2[] v22:
                    parameter.SetValue(v22);
                    return true;
                case Vector3 v3:
                    parameter.SetValue(v3);
                    return true;
                case Vector3[] v32:
                    parameter.SetValue(v32);
                    return true;
                case Color c:
                    parameter.SetValue(c.ToVector3());
                    return true;
                case Vector4 v4:
                    parameter.SetValue(v4);
                    return true;
                case Rectangle rect:
                    parameter.SetValue(new Vector4(rect.X, rect.Y, rect.Width, rect.Height));
                    return true;
                case Vector4[] v42:
                    parameter.SetValue(v42);
                    return true;
                case Matrix m:
                    parameter.SetValue(m);
                    return true;
                case Matrix[] m2:
                    parameter.SetValue(m2);
                    return true;
                case Texture2D t:
                    parameter.SetValue(t);
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            AdditionsMain.Instance.Logger.Error(
                $"ruh roh the shader {Name} tried to set a parameter with an unsupported type of {value.GetType().Name}");
            return false;
        }
    }

    /// <summary>
    /// Sets a texture at a given index for this shader to use <br></br>
    /// Remember, index 0 may be populated with whatever was passed into a <see cref="SpriteBatch"/>.Draw call
    /// </summary>
    /// <param name="texture">The texture to supply</param>
    /// <param name="textureIndex">The index to place the texture in</param>
    /// <param name="samplerStateOverride">Which sampler should be used for the texture</param>
    public void SetTexture(Texture2D texture, int textureIndex, SamplerState samplerStateOverride = null)
    {
        // Shaders do not work on servers
        if (Main.dedServ)
            return;

        // Try to send texture sizes as parameters
        TrySetParameter($"{TextureSizeParameterPrefix}{textureIndex}", texture.Size());

        // Grab the graphics device and send the texture to it
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.Textures[textureIndex] = texture;
        if (samplerStateOverride is not null)
            gd.SamplerStates[textureIndex] = samplerStateOverride;
    }

    /// <summary>
    /// Prepares the shader for drawing
    /// </summary>
    public void Render(string passName = DefaultPassName, bool pixelated = true, bool commonParams = true,
        Matrix? matrix = null)
    {
        // Shaders do not work on servers. If this method is called on one, terminate it immediately.
        if (Main.dedServ || Disposed)
            return;

        if (commonParams)
        {
            TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        }

        if (matrix == null)
            UpdateProjectiveMatrices(pixelated);
        else
        {
            TrySetParameter("transformMatrix", matrix.Value);
        }

        Effect.CurrentTechnique.Passes[passName].Apply();
    }

    public void UpdateProjectiveMatrices(bool pixelated = true)
    {
        Matrix world;
        Matrix view;
        Matrix projection;

        if (pixelated)
            GetPixelated2DMatrices(out world, out projection, out view);
        else
            Get2DMatrices(out world, out projection, out view);
        Matrix transform = world * view * projection;

        TrySetParameter("transformMatrix", transform);
    }

    ~ManagedShader()
    {
        if (Disposed)
            return;

        Disposed = true;
        Effect.Dispose();
        ParameterCache.Clear();
    }

    public void Dispose()
    {
        if (Disposed)
            return;

        Disposed = true;
        Effect.Dispose();
        ParameterCache.Clear();
        GC.SuppressFinalize(this);
    }
}