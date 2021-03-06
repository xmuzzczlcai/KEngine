﻿#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CBuild_UGUI.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System.IO;
using KEngine.UI;
using KUnityEditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KEngine.Editor
{
    [InitializeOnLoad]
    public class KUGUIBuilder : KBuild_Base
    {
        static KUGUIBuilder()
        {
            KUnityEditorEventCatcher.OnWillPlayEvent -= OnWillPlayEvent;
            KUnityEditorEventCatcher.OnWillPlayEvent += OnWillPlayEvent;

        }

        private static void OnWillPlayEvent()
        {
            // Auto Link resources when play!
            if (!Directory.Exists(ResourcesSymbolLinkHelper.GetLinkPath()))
            {
                KLogger.LogWarning("Auto Link Bundle Resources Path... {0}", ResourcesSymbolLinkHelper.GetLinkPath());
                ResourcesSymbolLinkHelper.SymbolLinkResource();
            }
        }

        [MenuItem("KEngine/UI(UGUI)/Export Current UI %&e")]
        public static void ExportCurrentUI()
        {
            if (EditorApplication.isPlaying)
            {
                KLogger.LogError("Cannot export in playing mode! Please stop!");
                return;
            }

            //var UIName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
            var windowAssets = GameObject.FindObjectsOfType<KUIWindowAsset>();
            if (windowAssets.Length <= 0)
            {
                KLogger.LogError("Not found KUIWindowAsset in scene `{0}`", EditorApplication.currentScene);
            }
            else
            {
                foreach (var windowAsset in windowAssets)
                {
                    var uiName = windowAsset.name;
                    BuildTools.BuildAssetBundle(windowAsset.gameObject, GetBuildRelPath(uiName));
                }
            }
        }

        public static string GetBuildRelPath(string uiName)
        {
            return string.Format("UI/{0}_UI{1}", uiName, KEngine.AppEngine.GetConfig("AssetBundleExt"));
        }

        [MenuItem("KEngine/UI(UGUI)/Create UI(UGUI)")]
        public static void CreateNewUI()
        {
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null)
                GameObject.DestroyImmediate(mainCamera);

            var uiName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
            if (string.IsNullOrEmpty(uiName) || GameObject.Find(uiName) != null) // default use scene name, if exist create random name
            {
                uiName = "NewUI_" + Path.GetRandomFileName();
            }
            GameObject uiObj = new GameObject(uiName);
            uiObj.layer = (int)UnityLayerDef.UI;
            uiObj.AddComponent<KUIWindowAsset>();

            var uiPanel = new GameObject("Image").AddComponent<Image>();
            uiPanel.transform.parent = uiObj.transform;
            KTool.ResetLocalTransform(uiPanel.transform);

            var canvas = uiObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiObj.AddComponent<CanvasScaler>();
            uiObj.AddComponent<GraphicRaycaster>();

            if (GameObject.Find("EventSystem") == null)
            {
                var evtSystemObj = new GameObject("EventSystem");
                evtSystemObj.AddComponent<EventSystem>();
                evtSystemObj.AddComponent<StandaloneInputModule>();
                evtSystemObj.AddComponent<TouchInputModule>();

            }

            if (GameObject.Find("Camera") == null)
            {
                GameObject cameraObj = new GameObject("Camera");
                cameraObj.layer = (int)UnityLayerDef.UI;

                Camera camera = cameraObj.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.depth = 0;
                camera.backgroundColor = Color.grey;
                camera.cullingMask = 1 << (int)UnityLayerDef.UI;
                camera.orthographicSize = 1f;
                camera.orthographic = true;
                camera.nearClipPlane = -2f;
                camera.farClipPlane = 2f;

                camera.gameObject.AddComponent<AudioListener>();

            }

            Selection.activeGameObject = uiObj;
        }

        public override void Export(string path)
        {
            EditorApplication.OpenScene(path);

            ExportCurrentUI();
        }

        public override string GetDirectory()
        {
            return "UI";
        }

        public override string GetExtention()
        {
            return "*.unity";
        }
    }
}