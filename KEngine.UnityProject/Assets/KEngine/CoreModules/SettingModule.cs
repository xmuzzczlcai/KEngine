﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using CosmosTable;

namespace KEngine.Modules
{
    /// <summary>
    /// Unity SettingModule, with Resources.Load in product,  with File.Read in editor
    /// </summary>
    public class SettingModule : SettingModuleBase
    {
        public delegate byte[] LoadSettingFuncDelegate(string filePath);
        public delegate byte[] SettingBytesFilterDelegate(byte[] bytes);

        /// <summary>
        /// Filter the loaded bytes, which settings file may be encrypted, so you can manipulate the bytes
        /// </summary>
        public static SettingBytesFilterDelegate SettingBytesFilter;

        /// <summary>
        /// Override the default load file strategy
        /// </summary>
        public static LoadSettingFuncDelegate CustomLoadSetting;

        private static readonly bool IsEditor;
        static SettingModule()
        {
            IsEditor = Application.isEditor;
        }

        /// <summary>
        /// internal constructor
        /// </summary>
        internal SettingModule()
        {
        }

        /// <summary>
        /// Load KEngineConfig.txt 's `SettingPath`
        /// </summary>
        protected static string SettingFolderName
        {
            get
            {
                return AppEngine.GetConfig("SettingResourcesPath");
            }
        }

        /// <summary>
        /// Singleton
        /// </summary>
        private static SettingModule _instance;

        /// <summary>
        /// Quick method to get TableFile from instance
        /// </summary>
        /// <param name="path"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static TableFile Get(string path, bool useCache = true)
        {
            if (_instance == null)
                _instance = new SettingModule();
            return _instance.GetTableFile(path, useCache);
        }

        /// <summary>
        /// Unity Resources.Load setting file in Resources folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected override string LoadSetting(string path)
        {
            byte[] fileContent = CustomLoadSetting != null ? CustomLoadSetting(path) : DefaultLoadSetting(path);
            return Encoding.UTF8.GetString(fileContent);
        }

        /// <summary>
        /// Default load setting strategry,  editor load file, runtime resources.load
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] DefaultLoadSetting(string path)
        {
            byte[] fileContent;

            if (IsFileSystemMode)
                fileContent = LoadSettingFromFile(path);
            else
                fileContent = LoadSettingFromResources(path);
            return fileContent;
        }

        /// <summary>
        /// Load setting in file system using `File` class
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static byte[] LoadSettingFromFile(string path)
        {
            var resPath = GetFileSystemPath(path);
            var bytes = File.ReadAllBytes(resPath);
            bytes = SettingBytesFilter != null ? SettingBytesFilter(bytes) : bytes;
            return bytes;
        }

        private static string GetFileSystemPath(string path)
        {
            var resPath = "Assets/Resources/" + SettingFolderName + "/" + path;
            return resPath;
        }

        /// <summary>
        /// Cache all the FileSystemWatcher, prevent the duplicated one
        /// </summary>
        private static Dictionary<string, FileSystemWatcher> _cacheWatchers;

        /// <summary>
        /// Watch the setting file, when changed, trigger the delegate
        /// </summary>
        /// <param name="path"></param>
        /// <param name="action"></param>
        public static void WatchSetting(string path, System.Action<string> action)
        {
            if (!IsFileSystemMode)
            {
                KLogger.LogError("[WatchSetting] Available in Unity Editor mode only!");
                return;
            }
            if (_cacheWatchers == null)
                _cacheWatchers = new Dictionary<string, FileSystemWatcher>();
            FileSystemWatcher watcher;
            var dirPath = Path.GetDirectoryName(GetFileSystemPath(path));
            if (!_cacheWatchers.TryGetValue(dirPath, out watcher))
                _cacheWatchers[dirPath] = watcher = new FileSystemWatcher(dirPath);

            watcher.IncludeSubdirectories = false;
            watcher.Path = dirPath;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*";
            watcher.EnableRaisingEvents = true;
            watcher.InternalBufferSize = 2048;
            watcher.Changed += (sender, e) =>
            {
                action(path);
            };
        }

        /// <summary>
        /// Load from unity Resources folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static byte[] LoadSettingFromResources(string path)
        {
            var resPath = SettingFolderName + "/" + Path.ChangeExtension(path, null);
            var fileContentAsset = Resources.Load(resPath) as TextAsset;
            var bytes = SettingBytesFilter != null ? SettingBytesFilter(fileContentAsset.bytes) : fileContentAsset.bytes;
            return bytes;
        }

        /// <summary>
        /// whether or not using file system file, in unity editor mode only
        /// </summary>
        public static bool IsFileSystemMode
        {
            get
            {
                if (IsEditor)
                    return true;
                return false;

            }
        }
    }
}
