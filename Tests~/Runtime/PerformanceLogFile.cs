using System.Globalization;
using System.IO;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    internal class PerformanceLogFile
    {
        private const string FileName = "TestTimings.txt";

        private readonly string path;

        public PerformanceLogFile()
        {
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "VehicleCameraBenchmark");
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, FileName);
        }

        public void Append(string line)
        {
            string stamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            File.AppendAllText(path, $"{stamp} [{GetCodeOptimization()}] {line}{System.Environment.NewLine}");
        }

        private string GetCodeOptimization()
        {
#if UNITY_EDITOR
            return UnityEditor.Compilation.CompilationPipeline.codeOptimization.ToString();
#else
            return "Player";
#endif
        }
    }
}
