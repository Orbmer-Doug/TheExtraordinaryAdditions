using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Graphics.Shaders;

#if DEBUG

/// <summary>
/// The shader recompilation manager, which is responsible for ensuring that changes to .fx files are reflected in-game automatically.
/// </summary>
/// Main credit of this goes to Luminance made by Lucille Karma
public sealed class ShaderRecompilationMonitor : ModSystem
{
    /// <summary>
    /// An arbitrary number to cap the maximum compiled files at once to prevent extreme memory bloat
    /// </summary>
    private const int MaxCompilingFiles = 100;
    public static readonly Queue<CompilingFile> CompilingFiles = [];
    private static readonly List<ShaderWatcher> ShaderWatchers = [];
    private static readonly Dictionary<string, DateTime> DebounceTimestamps = [];

    // I'm unsure what causes it, if either tmod or git operations, but sometimes a grillion files get triggered and cause a mass reload
    private static readonly Queue<DateTime> QueueTimestamps = [];
    private static bool BurstDetected = false;

    /// <summary>
    /// A 500ms delay between queuing to prevent rapid-fire events
    /// </summary>
    private const double DebounceDelaySeconds = 0.5;
    private const double BurstDetectionWindowSeconds = 1.0;
    private const int BurstThreshold = 50;

    public static string CompilerDirectory => Path.Combine(Main.SavePath, "FXC");

    public record ShaderWatcher(string EffectsPath, string ModName, FileSystemWatcher FileWatcher);
    public record CompilingFile(string FilePath, bool CompileAsFilter);

    public override void PostUpdateEverything()
    {
        if (BurstDetected)
        {
            // Skip processing if a burst was detected
            lock (CompilingFiles)
            {
                if (CompilingFiles.Count > 0)
                {
                    Main.NewText("Skipping shader compilation due to excessive file changes.", Color.OrangeRed);
                    CompilingFiles.Clear();
                    BurstDetected = false; // Reset burst detection after clearing
                }
            }
            return;
        }

        foreach (ShaderWatcher watcher in ShaderWatchers)
            ProcessCompilationsForWatcher(watcher);
    }

    public override void PostSetupContent()
    {
        lock (CompilingFiles)
        {
            CompilingFiles.Clear();
        }
        lock (QueueTimestamps)
        {
            QueueTimestamps.Clear();
        }
        BurstDetected = false;
    }

    public override void OnModLoad()
    {
        ClearCompilationDirectory();
        if (Directory.Exists(CompilerDirectory) || Main.netMode != NetmodeID.SinglePlayer)
            return;

        CreateCompilerDirectory();
    }

    public override void OnModUnload()
    {
        foreach (ShaderWatcher watcher in ShaderWatchers)
            watcher.FileWatcher?.Dispose();
        ShaderWatchers.Clear();
        DebounceTimestamps.Clear();
        lock (QueueTimestamps)
        {
            QueueTimestamps.Clear();
        }
        BurstDetected = false;
    }

    internal void CreateCompilerDirectory()
    {
        Directory.CreateDirectory(CompilerDirectory);
        List<string> fileNames = Mod.GetFileNames();
        IEnumerable<string> compilerFiles = fileNames.Where(f => f.Contains("Assets/AutoloadedEffects/Compiler"));
        foreach (string fileName in compilerFiles)
        {
            byte[] fileData = Mod.GetFileBytes(fileName);
            string copyFileName = Path.Combine(CompilerDirectory, Path.GetFileName(fileName));
            File.WriteAllBytes(copyFileName, fileData);
        }
    }

    internal static void LoadForMod(Mod mod)
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
            return;

        string modSourcesPath = Path.Combine(Program.SavePathShared, "ModSources", mod.Name);
        if (!Directory.Exists(modSourcesPath))
            return;

        string effectsPath = Path.Combine(modSourcesPath, "Assets", "AutoloadedEffects");
        if (!Directory.Exists(effectsPath))
            return;

        // Restore separate watchers for Shaders and Filters
        string shadersPath = Path.Combine(effectsPath, "Shaders");
        string filtersPath = Path.Combine(effectsPath, "Filters");
        TryToWatchPath(mod, shadersPath);
        TryToWatchPath(mod, filtersPath);
    }

    private static void ClearCompilationDirectory()
    {
        if (!Directory.Exists(CompilerDirectory))
            return;

        foreach (string file in Directory.GetFiles(CompilerDirectory, "*.fx")
            .Concat(Directory.GetFiles(CompilerDirectory, "*.xnb"))
            .Concat(Directory.GetFiles(CompilerDirectory, "*.fxc")))
        {
            try { File.Delete(file); } catch { }
        }
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
                          NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security
        };
        watcher.Changed += MarkFileAsNeedingCompilation;
        ShaderWatchers.Add(new(path, mod.Name, watcher));
    }

    private static void ProcessCompilationsForWatcher(ShaderWatcher watcher)
    {
        List<CompilingFile> filesToCompile = new();
        lock (CompilingFiles)
        {
            while (CompilingFiles.Count > 0 && filesToCompile.Count < MaxCompilingFiles)
            {
                if (!CompilingFiles.TryDequeue(out CompilingFile file))
                    break;

                if (file.FilePath.Contains(watcher.ModName) && !filesToCompile.Any(f => f.FilePath == file.FilePath))
                    filesToCompile.Add(file);
                // No break; process all relevant files
            }
        }

        foreach (CompilingFile file in filesToCompile)
        {
            MoveFileToCompilingFolder(file);
            CompileFile(file);
            ProcessCompiledFile(file, watcher);
        }
    }

    private static void CompileFile(CompilingFile file)
    {
        string fxPath = Path.GetFileName(file.FilePath);
        string outputPath = Path.Combine(CompilerDirectory, Path.GetFileNameWithoutExtension(fxPath) + ".fxc");
        string args = $"/T fx_2_0 \"{fxPath}\" /Fo \"{outputPath}\"";

        Process fxcCompiler = new()
        {
            StartInfo = new(Path.Combine(CompilerDirectory, "fxc.exe"))
            {
                WorkingDirectory = CompilerDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = args
            }
        };

        fxcCompiler.Start();
        if (!fxcCompiler.WaitForExit(2500))
        {
            Main.NewText("Shader compiler timed out.", Color.OrangeRed);
            fxcCompiler.Kill();
            return;
        }

        string error = fxcCompiler.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            foreach (string line in error.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("implicit truncation") || line.Contains("Effects deprecated")) // shut up
                    continue;
                Main.NewText(line, Color.OrangeRed);
                line.Log();
            }
        }

        fxcCompiler.Dispose();
    }

    private static void ProcessCompiledFile(CompilingFile file, ShaderWatcher watcher)
    {
        string shaderPath = file.FilePath;
        string compiledFxcPath = Path.Combine(CompilerDirectory, Path.GetFileNameWithoutExtension(shaderPath) + ".fxc");
        string originalFxcPath = shaderPath.Replace(".fx", ".fxc");

        try
        {
            if (File.Exists(originalFxcPath))
                File.Delete(originalFxcPath);
            File.Move(compiledFxcPath, originalFxcPath);
            string oldXnbPath = shaderPath.Replace(".fx", ".xnb");
            if (File.Exists(oldXnbPath))
                File.Delete(oldXnbPath);
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to process '{Path.GetFileName(shaderPath)}': {ex.Message}", Color.OrangeRed);
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
                if (AssetRegistry.TryGetFilter(shaderId, out ManagedScreenShader oldFilter))
                    oldFilter.Shader = refEffect;
                else
                    AssetRegistry.SetFilter(shaderId, refEffect);
            }
            else
            {
                AssetRegistry.SetShader(shaderId, refEffect);
            }

            if (!file.CompileAsFilter)
                Main.NewText($"Shader '{shaderId}' recompiled successfully.");
            else
                Main.NewText($"Filter '{shaderId}' recompiled successfully. Rebuild to apply changes.");
        });
    }

    private static void MoveFileToCompilingFolder(CompilingFile file)
    {
        string destPath = Path.Combine(CompilerDirectory, Path.GetFileName(file.FilePath));
        try
        {
            File.Copy(file.FilePath, destPath, true);
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to copy '{Path.GetFileName(file.FilePath)}' to compiler: {ex.Message}", Color.OrangeRed);
        }
    }

    private static void MarkFileAsNeedingCompilation(object sender, FileSystemEventArgs e)
    {
        if (!AssetRegistry.HasFinishedLoading || e.FullPath.Contains("\\Compiler"))
            return;

        string filePath = e.FullPath;
        lock (DebounceTimestamps)
        {
            if (DebounceTimestamps.TryGetValue(filePath, out DateTime lastTime) &&
                (DateTime.Now - lastTime).TotalSeconds < DebounceDelaySeconds)
                return;

            DebounceTimestamps[filePath] = DateTime.Now;
        }

        lock (QueueTimestamps)
        {
            QueueTimestamps.Enqueue(DateTime.Now);

            while (QueueTimestamps.Count > 0 && (DateTime.Now - QueueTimestamps.Peek()).TotalSeconds > BurstDetectionWindowSeconds)
                QueueTimestamps.Dequeue();

            if (QueueTimestamps.Count > BurstThreshold)
            {
                BurstDetected = true;
                Main.NewText("Detected excessive shader file changes. Compilation queue cleared to prevent overload.", Color.OrangeRed);
                lock (CompilingFiles)
                {
                    CompilingFiles.Clear();
                }
                return;
            }
        }

        lock (CompilingFiles)
        {
            if (CompilingFiles.Count >= MaxCompilingFiles)
            {
                Main.NewText("Too many shader compilations queued.", Color.OrangeRed);
                return;
            }
            if (!CompilingFiles.Any(f => f.FilePath == filePath))
                CompilingFiles.Enqueue(new(filePath, filePath.Contains("\\Filters")));
        }
    }
}
#endif