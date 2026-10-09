using UnityEditor;
using UnityEngine;
using System.IO;

namespace FrioAndino.Editor
{
    public static class BuildScript
    {
        [MenuItem("FrioAndino/Build WebGL")]
        public static void BuildWebGL()
        {
            string buildPath = "Builds/WebGL";
            if (Directory.Exists(buildPath))
            {
                try { Directory.Delete(buildPath, true); } catch {}
            }
            Directory.CreateDirectory(buildPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Unity Play funciona mejor sin compresion o con Gzip directo
            PlayerSettings.productName = "Frio Andino - Sobrevivir a la Helada";
            PlayerSettings.companyName = "Universidad Continental";

            string[] scenes = new string[] { "Assets/Scenes/FrioAndino.unity" };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[FrioAndino] WebGL build completado exitosamente en: {buildPath}");
            }
            else
            {
                Debug.LogError($"[FrioAndino] Error en el build WebGL: {report.summary.result}, Errores: {report.summary.totalErrors}");
            }
        }
    }
}
