#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Reproducible editor setup, puzzle checks and Windows build commands.</summary>
public static class AetherBuild
{
    public const string ScenePath = "Assets/Scenes/Sanctuary.unity";

    [MenuItem("AETHER/Prepare project")]
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Aether Studio";
        PlayerSettings.productName = "AETHER";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        // Keep the project's built-in Input Manager backend for keyboard/mouse controls.
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settings.Length > 0)
        {
            var serializedSettings = new SerializedObject(settings[0]);
            var inputHandler = serializedSettings.FindProperty("activeInputHandler");
            if (inputHandler != null)
            {
                inputHandler.intValue = 0;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        // These shaders are selected by name when the runtime creates the world.
        // Keep them in the player even though the bootstrap scene contains no materials.
        var graphicsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (graphicsAssets.Length > 0)
        {
            var graphics = new SerializedObject(graphicsAssets[0]);
            var included = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (included != null)
            {
                foreach (string shaderName in new[] { "Standard", "Legacy Shaders/Particles/Additive", "Aether/Atmosphere" })
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader == null) throw new InvalidOperationException("Required shader is missing: " + shaderName);
                    bool present = false;
                    for (int i = 0; i < included.arraySize; i++)
                        if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                    if (!present)
                    {
                        included.InsertArrayElementAtIndex(included.arraySize);
                        included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                    }
                }
                graphics.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 90f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.pixelLightCount = 8;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 1;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHER_PROJECT_READY: Unity project configured; bootstrap scene is ready.");
    }

    [MenuItem("AETHER/Validate puzzle logic")]
    public static void Validate()
    {
        Prepare();
        Type game = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes).FirstOrDefault(t => t.Name == "AetherGame");
        Check(game != null, "AetherGame runtime class exists");
        int assertions = 0;
        Action<bool, string> expect = (condition, label) => { Check(condition, label); assertions++; };
        Func<string, object[], object> call = (name, args) =>
        {
            var method = game.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Check(method != null, "Puzzle helper exists: " + name);
            return method.Invoke(null, args);
        };
        Func<int[], bool> rings = values => (bool)call("RingsSolved", new object[] { values });
        Func<int[], bool> sequence = values => (bool)call("IsSequenceCorrect", new object[] { values });
        Func<int, int, int> toggle = (mask, index) => (int)call("LightMaskAfterToggle", new object[] { mask, index });
        expect(rings(new[] { 2, 5, 3 }), "Correct ring combination opens the station");
        expect(!rings(new[] { 2, 5, 2 }), "Wrong ring combination is rejected");
        expect(!rings(new[] { 2, 5 }), "Short ring combination is rejected");
        expect(!rings(new[] { 2, 5, 3, 0 }), "Long ring combination is rejected");
        expect(!rings(null), "Null ring state is rejected");
        expect(sequence(new[] { 0, 2, 1, 3 }), "Correct echo sequence is accepted");
        expect(!sequence(new[] { 0, 1, 2, 3 }), "Incorrect echo ordering is rejected");
        expect(!sequence(new[] { 0, 2, 1 }), "Incomplete echo sequence is rejected");
        expect(!sequence(new[] { 0, 2, 1, 3, 0 }), "Overlong echo sequence is rejected");
        expect(!sequence(null), "Null echo sequence is rejected");
        for (int mask = 0; mask < 8; mask++)
        {
            for (int index = 0; index < 3; index++)
            {
                int expected = mask ^ (1 << index) ^ (1 << ((index + 1) % 3));
                expect(toggle(mask, index) == expected, "Light toggle changes exactly two adjacent lamps");
                expect(toggle(toggle(mask, index), index) == mask, "Repeating a light toggle restores the original state");
            }
        }
        var visited = new HashSet<int> { 1 };
        var states = new Queue<int>();
        states.Enqueue(1);
        while (states.Count > 0)
        {
            int state = states.Dequeue();
            for (int index = 0; index < 3; index++)
            {
                int next = toggle(state, index);
                if (visited.Add(next)) states.Enqueue(next);
            }
        }
        expect(visited.Contains(7), "Light puzzle can reach all-on from its initial state");
        expect(File.Exists(ScenePath), "Bootstrap scene exists");
        expect(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), "Bootstrap scene is included in builds");
        expect(PlayerSettings.productName == "AETHER", "Player product name is configured");
        foreach(string name in new[]{"AetherExpansion","AetherFrontiers","AetherHorizons"})
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetLoadableTypes).FirstOrDefault(t=>t.Name==name);
            expect(type!=null,name+" runtime class exists");
            var method=type.GetMethod("RunSelfTests",BindingFlags.Public|BindingFlags.Static);
            expect(method!=null,name+" rule checks exist");
            foreach(string result in (IEnumerable<string>)method.Invoke(null,null))expect(result.StartsWith("PASS "),result);
        }
        Debug.Log("AETHER_VALIDATION_PASSED: " + assertions + " assertions passed.");
    }

    [MenuItem("AETHER/Build Windows player")]
    public static void BuildWindows()
    {
        Validate();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Aether-Windows"));
        Directory.CreateDirectory(output);
        EditorSceneManager.SaveOpenScenes();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(output, "Aether.exe"),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        });
        var summary = report.summary;
        var result = new BuildReportData
        {
            product = "AETHER",
            unityVersion = Application.unityVersion,
            result = summary.result.ToString(),
            errors = summary.totalErrors,
            warnings = summary.totalWarnings,
            bytes = summary.totalSize.ToString(),
            seconds = summary.totalTime.TotalSeconds,
            executable = summary.outputPath,
            completedUtc = DateTime.UtcNow.ToString("o"),
            warningsDetail = report.steps.SelectMany(step => step.messages).Where(message => message.type == LogType.Warning).Select(message => message.content).ToArray()
        };
        File.WriteAllText(Path.Combine(output, "buildreport.json"), JsonUtility.ToJson(result, true));
        Check(summary.result == BuildResult.Succeeded, "Windows player build succeeded");
        Debug.Log("AETHER_BUILD_SUCCEEDED: " + summary.outputPath);
    }

    static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException exception) { return exception.Types.Where(t => t != null); }
    }

    [MenuItem("AETHER/Build and verify journey %&F9")]
    public static void BuildAndVerify()
    {
        BuildWindows();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Aether-Windows"));
        string reports=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation"));
        Directory.CreateDirectory(reports);
        string previousReport=Path.Combine(reports,"runtime-validation.json");
        if(File.Exists(previousReport))
        {
            File.Copy(previousReport,Path.Combine(reports,"runtime-validation-previous.json"),true);
            File.Delete(previousReport);
        }
        var start=new System.Diagnostics.ProcessStartInfo
        {
            FileName=Path.Combine(output,"Aether.exe"),
            Arguments="-aether-smoke -aether-report \""+reports+"\" -logFile \""+Path.Combine(reports,"player.log")+"\" -screen-width 1600 -screen-height 900 -screen-fullscreen 0",
            WorkingDirectory=output,
            UseShellExecute=false
        };
        System.Diagnostics.Process.Start(start);
        Debug.Log("AETHER: Windows journey validation started. Reports: "+reports);
    }

    [MenuItem("AETHER/Play sanctuary")]
    public static void PlaySanctuary() { Prepare();EditorApplication.isPlaying=true; }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("AETHER_VALIDATION_FAILED: " + message);
    }

    [Serializable]
    class BuildReportData
    {
        public string product, unityVersion, result, bytes, executable, completedUtc;
        public string[] warningsDetail;
        public int errors, warnings;
        public double seconds;
    }
}
#endif



