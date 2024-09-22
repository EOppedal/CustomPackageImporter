#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace CustomPackageImporter.Editor {
    public class CustomPackageImporter : EditorWindow {
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        private const string ManifestPath = "Packages/manifest.json";
        private const string PackagePath = "/package.json";

        private const string CustomPackagesScrubPath =
            "Packages/com.erlend-eiken-oppedal.custompackageimporter/Editor/CustomPackages.asset";

        private const string IconPath = "Packages/com.erlend-eiken-oppedal.custompackageimporter/Editor/Icon.png";

        private JObject _manifestJson;
        private static readonly List<Button> Buttons = new();

        private JToken dependenciesJToken => _manifestJson["dependencies"];

        [MenuItem("Window/Custom Package Importer")]
        public static void ShowExample() {
            var wnd = GetWindow<CustomPackageImporter>("CustomPackageImporter");
            wnd.titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(IconPath);
        }

        private void Update() {
            if (_manifestJson == null) return;
            
            if (dependenciesJToken == null) {
                Debug.LogWarning("Dependencies not found in manifest.");
                return;
            }

            var urls = ExtractUrls(dependenciesJToken.ToString());

            foreach (var button in Buttons) {
                button.enabledSelf = urls.All(x => x != button.tooltip);
            }
        }

        private static List<string> ExtractUrls(string input) {
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
            importButton.clicked += () => _ = InstallGitPackage(textField.value);

            var customPackages = AssetDatabase.LoadAssetAtPath<CustomPackages>(CustomPackagesScrubPath);
            
            var shortCutContainer = rootVisualElement.Q<VisualElement>("unity-content-container");

            foreach (var customPackage in customPackages.packages) {
                CreateCustomPackageImportShortcut(customPackage, shortCutContainer);
            }
        }

        private async Task InstallGitPackage(string gitUrl) {
            const string repoPath = "Assets/tempFolder";

            try {
                await CloneRepository(gitUrl, repoPath);

                const string packageJsonPath = repoPath + PackagePath;
                if (!File.Exists(packageJsonPath)) {
                    Debug.LogError("package.json not found in the repository!");
                    return;
                }

                var json = await File.ReadAllTextAsync(packageJsonPath);
                var packageJson = JObject.Parse(json);

                TryDeleteTempRepo(repoPath);

                if (packageJson["dependencies"] is JObject dependencies) {
                    foreach (var dependency in dependencies) {
                        await InstallGitPackage(dependency.Value?.ToString());
                    }
                }

                _manifestJson["dependencies"]![packageJson["name"]?.ToString()!] = gitUrl;

                await File.WriteAllTextAsync(ManifestPath, _manifestJson.ToString());

                Debug.Log("Installation success!");
            }
            catch (Exception ex) {
                Debug.LogError($"An error occurred: {ex.Message}");
                TryDeleteTempRepo(repoPath);
            }
        }

        private void CreateCustomPackageImportShortcut(CustomPackages.CustomPackage package, VisualElement shortcutContainer) {
            var button = new Button {
                text = package.packageName
            };

            button.clicked += () => InstallGitPackageCallback(package);
            button.AddToClassList("button");
            button.tooltip = package.gitUrl;
            shortcutContainer.Add(button);
            Buttons.Add(button);
        }


        private void InstallGitPackageCallback(CustomPackages.CustomPackage package) => _ = InstallGitPackage(package.gitUrl);

        private static void TryDeleteTempRepo(string repoPath) {
            if (Directory.Exists(repoPath)) {
                FileUtil.DeleteFileOrDirectory(repoPath);
            }
        }

        private static async Task CloneRepository(string gitUrl, string clonePath) {
            var process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = $"clone {gitUrl} {clonePath}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                Debug.LogError($"Error cloning repository: {await process.StandardError.ReadToEndAsync()}");
            }
        }
    }
}
#endif