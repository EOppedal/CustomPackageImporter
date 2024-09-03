﻿#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Editor {
    public class CustomPackages : ScriptableObject {
        public CustomPackage[] packages;
        
        [Serializable] public struct CustomPackage {
            public string packageName;
            public string gitUrl;
        }
    }
}
#endif