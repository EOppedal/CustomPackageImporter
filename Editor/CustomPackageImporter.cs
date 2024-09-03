#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor {
    public class CustomPackageImporter : EditorWindow {
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        private const string ManifestPath = "Packages/manifest.json";
        private const string PackagePath = "/package.json";
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
            var repoPath = ResourcesPath + "tempRepo";

            try {
                CloneRepository(gitUrl, repoPath);

                var packageJsonPath = repoPath + PackagePath;
                if (!File.Exists(packageJsonPath)) {
                    UnityEngine.Debug.LogError("package.json not found in the repository!");
                    return;
                }

                var json = File.ReadAllText(packageJsonPath);
                var packageJson = JObject.Parse(json);

                // If no dependencies, skip the manifest update
                if (packageJson["dependencies"] is not JObject dependencies) return;

                // Read and update manifest.json
                var manifestJson = JObject.Parse(File.ReadAllText(ManifestPath));

                // Add each dependency to manifest.json
                foreach (var dependency in dependencies) {
                    manifestJson["dependencies"][dependency.Key] = dependency.Value;
                }

                // Add the main package to manifest.json
                manifestJson["dependencies"][packageJson["name"].ToString()] = gitUrl;

                // Write updated manifest.json back to the file
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