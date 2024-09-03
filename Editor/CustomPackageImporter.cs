#if UNITY_EDITOR
using System;
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
        private const string PackagePath = "/package.json";
        private const string CustomPackagesScrubPath = "Packages/com.erlend-eiken-oppedal.custompackageimporter/Editor/CustomPackages.asset";

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
            
            var customPackages = AssetDatabase.LoadAssetAtPath<CustomPackages>(CustomPackagesScrubPath);

            foreach (var customPackage in customPackages.packages) {
                CreateCustomPackageImportShortcut(customPackage);
            }
        }

        private static void InstallGitPackage(string gitUrl) {
            var manifestJson = JObject.Parse(File.ReadAllText(ManifestPath));
            if (manifestJson.TryGetValue(gitUrl, out var manifest)) {
                UnityEngine.Debug.LogWarning("Git package already exists in manifest.json");
                return;
            }
            
            const string repoPath = "Assets/tempRepo";

            try {
                CloneRepository(gitUrl, repoPath);

                var packageJsonPath = repoPath + PackagePath;
                if (!File.Exists(packageJsonPath)) {
                    UnityEngine.Debug.LogError("package.json not found in the repository!");
                    return;
                }

                var json = File.ReadAllText(packageJsonPath);
                var packageJson = JObject.Parse(json);

                if (packageJson["dependencies"] is not JObject dependencies) return;

                foreach (var dependency in dependencies) {
                    manifestJson["dependencies"]![dependency.Key] = dependency.Value;
                }

                manifestJson["dependencies"]![packageJson["name"]?.ToString()!] = gitUrl;

                File.WriteAllText(ManifestPath, manifestJson.ToString());

                UnityEngine.Debug.Log("Dependencies installed successfully!");
            }
            catch (Exception ex) {
                UnityEngine.Debug.LogError($"An error occurred: {ex.Message}");
            }
            finally {
                DeleteTempRepo(repoPath);
            }
        }

        private void CreateCustomPackageImportShortcut(CustomPackages.CustomPackage package) {
            var button = new Button {
                text = package.packageName
            };
            button.RegisterCallback<ClickEvent>(_ => InstallGitPackage(package.gitUrl));
            rootVisualElement.Add(button);
        }

        private static void DeleteTempRepo(string repoPath) {
            Awaitable.NextFrameAsync();
            
            if (Directory.Exists(repoPath)) {
                Directory.Delete(repoPath, true);
            }
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