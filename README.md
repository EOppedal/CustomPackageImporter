# CustomPackageImporter
Automatically adds a Unity package **and all of its dependencies** to the `manifest.json`.

## ⚠ Known Issues
- Unity does **not always immediately refresh** after modifying `manifest.json`.
  You'll need to click out of the Unity window and back in for the packages to download.
  This is due to Unity's package refresh behavior.
