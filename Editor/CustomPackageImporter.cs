#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor {
    public class CustomPackageImporter : EditorWindow {
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        private const string ManifestPath = "Packages/manifest.json";
        private const string PackagePath =  "/package.json";
        private const string ResourcesPath = "Assets/Resources";

        [MenuItem("Window/MyWindows/CustomPackageImporter")]
        public static void ShowExample() {
            var wnd = GetWindow<CustomPackageImporter>();
            wnd.titleContent = new GUIContent("CustomPackageImporter");
        }

        public void CreateGUI() {
            visualTreeAsset.CloneTree(rootVisualElement);

            var textField = rootVisualElement.Q<TextField>("TextField");
            var importButton = rootVisualElement.Q<Button>("ImportButton");
            importButton.RegisterCallback<ClickEvent>(_ => InstallGitPackage(textField.value));
        }

        private static void InstallGitPackage(string gitUrl) {
            var repoPath = ResourcesPath + "/tempRepo";
            CloneRepository(gitUrl, repoPath);

            var json = File.ReadAllText(Directory.GetCurrentDirectory() + PackagePath);
            var packageJson = JObject.Parse(json);

            if (packageJson["dependencies"] is not JObject dependencies) return;
            var manifestJson = JObject.Parse(File.ReadAllText(ManifestPath));

            foreach (var dependency in dependencies) {
                manifestJson["dependencies"][dependency.Key] = dependency.Value;
            }

            manifestJson["dependencies"][packageJson["name"]] = gitUrl;

            File.WriteAllText(ManifestPath, manifestJson.ToString());
            
            Directory.Delete(repoPath);
        }

        private static void CloneRepository(string gitUrl, string clonePath) {
            var process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = $"clone {gitUrl} {clonePath}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0) {
                UnityEngine.Debug.Log($"Repository cloned to: {clonePath}");
            }
            else {
                UnityEngine.Debug.LogError($"Error cloning repository: {process.StandardError.ReadToEnd()}");
            }
        }
    }
}
#endif