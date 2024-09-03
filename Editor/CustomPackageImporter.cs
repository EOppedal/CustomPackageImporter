#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Editor {
    public class CustomPackageImporter : EditorWindow {
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        private const string ManifestPath = "Packages/manifest.json";
        private const string PackagePath = "/package.json";
        private const string CustomPackagesScrubPath = "Packages/com.erlend-eiken-oppedal.custompackageimporter/Editor/CustomPackages.asset";
        private const string IconPath = "Packages/com.erlend-eiken-oppedal.custompackageimporter/Editor/Icon.png";
        
        private JObject _manifestJson;
        private static readonly List<Button> Buttons = new();
        
        [MenuItem("Window/Custom Package Importer")]
        public static void ShowExample() {
            var wnd = GetWindow<CustomPackageImporter>("CustomPackageImporter");
            wnd.titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(IconPath);
        }

        private void Update() {
            var dependencies = _manifestJson["dependencies"];
            if (dependencies == null) {
                Debug.LogWarning("Dependencies not found in manifest.");
                return;
            }

            var urls = ExtractUrls(dependencies.ToString());

            foreach (var button in Buttons) {
                button.enabledSelf = urls.All(x => x != button.tooltip);
                Debug.Log($"{button.tooltip} enabled: {button.enabledSelf}");
            }
        }

        static List<string> ExtractUrls(string input) {
            var urlList = new List<string>();
            var regex = new Regex(@"https?:\/\/[^\s""']+");
            var matches = regex.Matches(input);

            foreach (Match match in matches) {
                urlList.Add(match.Value);
            }

            return urlList;
        }

        public void CreateGUI() {
            visualTreeAsset.CloneTree(rootVisualElement);

            var textField = rootVisualElement.Q<TextField>("TextField");
            var importButton = rootVisualElement.Q<Button>("ImportButton");
            
            _manifestJson = JObject.Parse(File.ReadAllText(ManifestPath));
            importButton.RegisterCallback<ClickEvent>(_ => InstallGitPackage(textField.value));
            
            var customPackages = AssetDatabase.LoadAssetAtPath<CustomPackages>(CustomPackagesScrubPath);

            foreach (var customPackage in customPackages.packages) {
                CreateCustomPackageImportShortcut(customPackage);
            }
        }

        private void InstallGitPackage(string gitUrl) {
            const string repoPath = "Assets/tempFolder";

            try {
                CloneRepository(gitUrl, repoPath);

                var packageJsonPath = repoPath + PackagePath;
                if (!File.Exists(packageJsonPath)) {
                    UnityEngine.Debug.LogError("package.json not found in the repository!");
                    return;
                }

                var json = File.ReadAllText(packageJsonPath);
                var packageJson = JObject.Parse(json);
                
                TryDeleteTempRepo(repoPath);

                if (packageJson["dependencies"] is JObject dependencies) {
                    foreach (var dependency in dependencies) {
                        InstallGitPackage(dependency.Value?.ToString());
                    }
                }

                _manifestJson["dependencies"]![packageJson["name"]?.ToString()!] = gitUrl;

                File.WriteAllText(ManifestPath, _manifestJson.ToString());

                UnityEngine.Debug.Log("Installation success!");
            }
            catch (Exception ex) {
                UnityEngine.Debug.LogError($"An error occurred: {ex.Message}");
                TryDeleteTempRepo(repoPath);
            }
        }

        private void CreateCustomPackageImportShortcut(CustomPackages.CustomPackage package) {
            var button = new Button {
                text = package.packageName
            };
            button.RegisterCallback<ClickEvent>(_ => InstallGitPackage(package.gitUrl));
            button.AddToClassList("button");
            button.tooltip = package.gitUrl;
            rootVisualElement.Add(button);
            Buttons.Add(button);
        }

        private static void TryDeleteTempRepo(string repoPath) {
            if (Directory.Exists(repoPath)) {
                FileUtil.DeleteFileOrDirectory(repoPath);
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