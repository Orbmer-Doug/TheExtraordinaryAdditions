using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;

namespace TheExtraordinaryAdditions.Core.Systems;

public sealed class ShaderHotReloading : ModSystem
{
    private const int MaxCompilingFiles = 100;
    public static readonly Queue<CompilingFile> CompilingFiles = [];
    private static readonly List<ShaderWatcher> ShaderWatchers = [];
    private static readonly Dictionary<string, DateTime> DebounceTimestamps = [];
    private static readonly Queue<DateTime> QueueTimestamps = [];
    private static bool BurstDetected = false;
    private const double DebounceDelaySeconds = 0.5;

    private const double BurstDetectionWindowSeconds = 1.0;
    private const int BurstThreshold = 50;

    public static string CompilerDirectory => Path.Combine(Main.SavePath,
        "ModSources/TheExtraordinaryAdditions/Assets/AutoloadedEffects/Compiler");

    public readonly record struct ShaderWatcher(string EffectsPath, string ModName, FileSystemWatcher FileWatcher);

    public readonly record struct CompilingFile(string FilePath, bool CompileAsFilter);

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
            return;

        if (BurstDetected)
        {
            if (CompilingFiles.Count > 0)
            {
                DirectlyDisplayText("too many file changes i ain't compiling shaders", Color.OrangeRed);
                CompilingFiles.Clear();
                BurstDetected = false;
            }

            return;
        }

        foreach (ShaderWatcher watcher in ShaderWatchers)
            ProcessCompilationsForWatcher(watcher);
    }

    public override void PostSetupContent()
    {
        CompilingFiles.Clear();
        QueueTimestamps.Clear();
        BurstDetected = false;
    }

    public override void OnModLoad()
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
            return;

        string modSourcesPath = Path.Combine(Program.SavePathShared, "ModSources", AdditionsMain.Instance.Name);
        if (!Directory.Exists(modSourcesPath))
            return;

        string effectsPath = Path.Combine(modSourcesPath, "Assets", "AutoloadedEffects");
        if (!Directory.Exists(effectsPath))
            return;

        string shadersPath = Path.Combine(effectsPath, "Shaders");
        string filtersPath = Path.Combine(effectsPath, "Filters");
        TryToWatchPath(AdditionsMain.Instance, shadersPath);
        TryToWatchPath(AdditionsMain.Instance, filtersPath);

        if (!Directory.Exists(CompilerDirectory))
            throw new DirectoryNotFoundException($"Could not find compiler directory at {CompilerDirectory}");
        ClearCompilationDirectory();
    }

    public override void OnModUnload()
    {
        foreach (ShaderWatcher watcher in ShaderWatchers)
            watcher.FileWatcher?.Dispose();
        ShaderWatchers.Clear();
        DebounceTimestamps.Clear();
        QueueTimestamps.Clear();
        BurstDetected = false;
    }

    private static void ClearCompilationDirectory()
    {
        if (!Directory.Exists(CompilerDirectory))
            return;

        foreach (string file in Directory.GetFiles(CompilerDirectory, "*.fx")
                     .Concat(Directory.GetFiles(CompilerDirectory, "*.fxc")))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }

    private static void MarkFileAsNeedingCompilation(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains("\\Compiler"))
            return;

        string filePath = e.FullPath;
        if (DebounceTimestamps.TryGetValue(filePath, out DateTime lastTime) &&
            (DateTime.Now - lastTime).TotalSeconds < DebounceDelaySeconds)
            return;

        DebounceTimestamps[filePath] = DateTime.Now;

        QueueTimestamps.Enqueue(DateTime.Now);

        while (QueueTimestamps.Count > 0 &&
               (DateTime.Now - QueueTimestamps.Peek()).TotalSeconds > BurstDetectionWindowSeconds)
            QueueTimestamps.Dequeue();

        if (QueueTimestamps.Count > BurstThreshold)
        {
            BurstDetected = true;
            DirectlyDisplayText("wayy too many shader file changes! clearing the queue to prevent overload",
                Color.OrangeRed);
            CompilingFiles.Clear();
            return;
        }

        if (CompilingFiles.Count >= MaxCompilingFiles)
        {
            DirectlyDisplayText("too many shader compilations queued", Color.OrangeRed);
            return;
        }

        if (CompilingFiles.All(f => f.FilePath != filePath))
            CompilingFiles.Enqueue(new(filePath, filePath.Contains("\\Filters")));
    }

    private static void TryToWatchPath(Mod mod, string path)
    {
        if (!Directory.Exists(path))
            return;

        FileSystemWatcher watcher = new(path)
        {
            Filter = "*.fx",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.FileName |
                           NotifyFilters.LastWrite | NotifyFilters.Size
        };
        watcher.Changed += MarkFileAsNeedingCompilation;
        ShaderWatchers.Add(new(path, mod.Name, watcher));
    }

    private static void ProcessCompilationsForWatcher(ShaderWatcher watcher)
    {
        List<CompilingFile> filesToCompile = [];
        while (CompilingFiles.Count > 0 && filesToCompile.Count < MaxCompilingFiles)
        {
            if (!CompilingFiles.TryDequeue(out CompilingFile file))
                break;

            if (file.FilePath.Contains(watcher.ModName) && filesToCompile.All(f => f.FilePath != file.FilePath))
                filesToCompile.Add(file);
        }

        foreach (CompilingFile file in filesToCompile)
        {
            if (!MoveFileToCompilingFolder(file))
                continue;
            if (!CompileFile(file))
                continue;
            ProcessCompiledFile(file, watcher);
        }
    }

    private static bool MoveFileToCompilingFolder(CompilingFile file)
    {
        string destPath = Path.Combine(CompilerDirectory,
            Path.GetFileName(file.FilePath) ?? throw new InvalidOperationException());
        try
        {
            File.Copy(file.FilePath, destPath, true);
            return true;
        }
        catch (Exception ex)
        {
            DirectlyDisplayText($"err i failed to copy '{Path.GetFileName(file.FilePath)}' to compiler: {ex.Message}",
                Color.OrangeRed);
            return false;
        }
    }

    private static bool CompileFile(CompilingFile file)
    {
        string fxPath = Path.GetFileName(file.FilePath);
        string outputPath = Path.GetFileNameWithoutExtension(fxPath) + ".fxc";
        string args = $"/T fx_2_0 \"{fxPath}\" /Fo \"{outputPath}\" /D FX=1 /O3 /Op /nologo";

        ProcessStartInfo fxcInfo = new(Path.Combine(CompilerDirectory, "fxc.exe"))
        {
            WorkingDirectory = CompilerDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = args
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const string overrides_key = "WINEDLLOVERRIDES";
            const string d3d_override = "d3dcompiler_47=n";

            if (!fxcInfo.Environment.TryGetValue(overrides_key, out string overrides) ||
                string.IsNullOrEmpty(overrides))
            {
                overrides = d3d_override;
            }
            else
            {
                overrides += ";" + d3d_override;
            }

            fxcInfo.Environment[overrides_key] = overrides;
        }

        Process fxcCompiler = new()
        {
            StartInfo = fxcInfo
        };

        fxcCompiler.Start();
        if (!fxcCompiler.WaitForExit(2500))
        {
            DirectlyDisplayText("compiler timed out", Color.OrangeRed);
            return false;
        }

        string error = fxcCompiler.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            foreach (string line in error.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("implicit truncation") || line.Contains("Effects deprecated") ||
                    line.Contains("libEGL") || line.Contains("pci") || line.Contains("ioctl") ||
                    line.Contains("0024") || line.Contains("ID3DX")) // shut up
                    continue;
                DirectlyDisplayText(line, Color.OrangeRed);
                line.Log();
            }
        }

        return true;
    }

    private static void ProcessCompiledFile(CompilingFile file, ShaderWatcher watcher)
    {
        string shaderPath = file.FilePath;
        string compiledFxcPath = Path.Combine(CompilerDirectory, Path.GetFileNameWithoutExtension(shaderPath) + ".fxc");
        if (!File.Exists(compiledFxcPath))
            return;

        string originalFxcPath = shaderPath.Replace(".fx", ".fxc");

        try
        {
            if (File.Exists(originalFxcPath))
                File.Delete(originalFxcPath);
            File.Move(compiledFxcPath, originalFxcPath);
        }
        catch (Exception ex)
        {
            DirectlyDisplayText($"Failed to process '{Path.GetFileName(shaderPath)}': {ex.Message}", Color.OrangeRed);
            return;
        }
        finally
        {
            File.Delete(Path.Combine(CompilerDirectory, Path.GetFileName(shaderPath)));
        }

        Main.QueueMainThreadAction(() =>
        {
            string shaderId = Path.GetFileNameWithoutExtension(shaderPath);
            byte[] shaderData = File.ReadAllBytes(originalFxcPath);
            Ref<Effect> refEffect = new(new Effect(Main.instance.GraphicsDevice, shaderData));

            if (file.CompileAsFilter)
            {
                AssetRegistry.GennedShaders.Filters[shaderId].Dispose();
                if (AssetRegistry.GennedShaders.Filters.TryGetValue(shaderId, out ManagedScreenShader oldFilter))
                    oldFilter.Shader = refEffect;
                else
                    AssetRegistry.GennedShaders.SetFilter(shaderId, refEffect);
            }
            else
            {
                AssetRegistry.GennedShaders.Shaders[shaderId].Dispose();
                AssetRegistry.GennedShaders.SetShader(shaderId, refEffect);
            }

            DirectlyDisplayText(!file.CompileAsFilter
                ? $"Shader '{shaderId}' recompiled successfully"
                : $"Filter '{shaderId}' recompiled successfully. Restart to apply changes.");
        });
    }
}
