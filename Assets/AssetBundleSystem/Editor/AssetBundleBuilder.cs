using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace AssetBundleSystem
{
    public class AssetBundleBuilder
    {
        const BuildAssetBundleOptions options =
            BuildAssetBundleOptions.DeterministicAssetBundle |
            BuildAssetBundleOptions.CollectDependencies |
            BuildAssetBundleOptions.UncompressedAssetBundle |
            BuildAssetBundleOptions.CompleteAssets;

        protected AssetBundleDataWriter _dataWriter;
        protected AssetBundlePathResolver _pathResolver;
        protected AssetBundleBuildConfig _config;
        protected BuildTarget _target;

        protected List<AssetBundleNode> _dependNodes = new List<AssetBundleNode>();

        protected string bundleDir
        {
            get
            {
                string res = _config.bundlePath;
                switch (_target)
                {
                    case BuildTarget.StandaloneWindows:
                        res += "/windows";
                        break;
                    case BuildTarget.iPhone:
                        res += "/ios";
                        break;
                    case BuildTarget.Android:
                        res += "/android";
                        break;
                    default:
                        res += "/other";
                        break;
                }
                return res;
            }
        }

        public AssetBundleBuilder(AssetBundlePathResolver resolver)
        {
            _pathResolver = resolver;

            InitDirs();
        }

        private void InitDirs()
        {
            //new DirectoryInfo(_pathResolver.bundleDir).Create();
            //new FileInfo(_pathResolver.CacheFilePath).Directory.Create();
        }

        public void SetDataWriter(AssetBundleDataWriter writer)
        {
            _dataWriter = writer;
        }

        public void Begin()
        {
            EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
            
            //AssetBundleUtils.Init();
            
            // 如果装了Perforce插件，要先禁用，防止生成的中间文件及bundle加入至pendinglist
            //P4Connect.Config.PerforceEnabled = false;
            
            // 清空StreamingAssets文件夹
            if (Directory.Exists(Application.streamingAssetsPath))
                Directory.Delete(Application.streamingAssetsPath, true);

            // 如果用tolua#/ulua做热更方案的话，复制lua文件为bytes文件
            //ToLuaMenu.ClearLuaBytesFiles(LuaSettings.luaDir);
            //ToLuaMenu.ClearLuaBytesFiles(LuaSettings.toluaLuaDir);
            //ToLuaMenu.CopyLuaBytesFiles(LuaSettings.luaDir, LuaSettings.luaDir);
            //ToLuaMenu.CopyLuaBytesFiles(LuaSettings.toluaLuaDir, LuaSettings.toluaLuaDir);
            
            AssetDatabase.Refresh();
        }

        private void AddTargets(AssetBundleConfigNode configNode)
        {
            if (!configNode.enabled)
                return;

            int idx = _config.allDependNodes.IndexOf(configNode);
            AssetBundleConfigNode nextConfigNode = idx >= 0 && idx < _config.allDependNodes.Count - 1 ? _config.allDependNodes[idx + 1] : null;
            bool hasChild = nextConfigNode != null && nextConfigNode.level > configNode.level;
            AssetBundleNode node = new AssetBundleNode(configNode.ID, configNode.level, configNode.isIndependent, hasChild);

            List<string> assetPaths = new List<string>();
            foreach (AssetBundleFilter filter in configNode.filters)
            {
                if (!filter.valid)
                    continue;

                string filterPath = AssetBundleUtils.ConvertToAbsolutePath(configNode.dir) + "/" + filter.path;
                if (Directory.Exists(filterPath))
                {
                    string[] filterList = filter.filter.Split(';');
                    foreach (string fil in filterList)
                    {
                        string[] paths = Directory.GetFiles(filterPath, fil, filter.searchOption);
                        foreach (string path in paths)
                        {
                            if (path.Contains("\\Editor\\") || path.EndsWith(".meta"))
                                continue;

                            assetPaths.Add(AssetBundleUtils.ConvertToAssetPath(path));
                        }
                    }
                }
                else
                {
                    assetPaths.Add(configNode.dir + "/" + filter.path);
                }
            }

            if (configNode.exportType == ExportType.Whole)
            {
                AssetBundleTarget at = new AssetBundleTarget(configNode.name, configNode.dir, node, null, ref assetPaths);
                node.targets.Add(at);
            }
            else
            {
                foreach (var path in assetPaths)
                {
                    AssetBundleTarget at = new AssetBundleTarget(configNode.name, configNode.dir, node, path, ref assetPaths);
                    node.targets.Add(at);
                }
            }

            _dependNodes.Add(node);
        }

        public void AddTargets(AssetBundleBuildConfig config)
        {
            _config = config;
            _config.allDependNodes.ForEach(o => AddTargets(o));
            _dependNodes.ForEach(n => n.targets.ForEach(o => o.Analyze(ref _dependNodes)));
        }

        public void Export(BuildTarget target)
        {
            try
            {
                _target = target;
                Export();
                SaveDependInfo();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Export()
        {
            // 清空StreamingAssets文件夹
            if (Directory.Exists(Application.streamingAssetsPath))
                Directory.Delete(Application.streamingAssetsPath, true);
            Directory.CreateDirectory(Application.streamingAssetsPath);

            // 清空bundle文件夹
            if (Directory.Exists(bundleDir))
                Directory.Delete(bundleDir, true);
            Directory.CreateDirectory(bundleDir);

            Stack<int> independentStack = new Stack<int>();

            int preLevel = -1;
            foreach (AssetBundleNode node in _dependNodes)
            {
                if (node.level > preLevel)
                {
                    BuildPipeline.PushAssetDependencies();
                }
                else
                {
                    for (int ii = 0; ii < preLevel - node.level; ++ii)
                    {
                        BuildPipeline.PopAssetDependencies();
                    }
                }

                while (independentStack.Count > 0 && node.level <= independentStack.Peek())
                {
                    independentStack.Pop();
                    BuildPipeline.PopAssetDependencies();
                }

                if (node.isIndependent && node.hasChild)
                {
                    independentStack.Push(node.level);
                    BuildPipeline.PushAssetDependencies();
                }

                preLevel = node.level;

                node.targets.ForEach(o => o.WriteBundle(options, _target, bundleDir));
            }

            for (int ii = 0; ii <= preLevel; ++ii)
            {
                BuildPipeline.PopAssetDependencies();
            }
        }

        private void SaveDependInfo()
        {
            string streamPath = Path.Combine(Application.streamingAssetsPath, _pathResolver.dependInfoFileName);
            
            if (File.Exists(streamPath))
                File.Delete(streamPath);

            List<AssetBundleTarget> allTargets = new List<AssetBundleTarget>();
            _dependNodes.ForEach(n => n.targets.ForEach(t => allTargets.Add(t)));
            _dataWriter.Save(streamPath, allTargets.ToArray());

            string path = Path.Combine(bundleDir, _pathResolver.dependInfoFileName);
            File.Copy(streamPath, path, true);
        }

        private void SaveCache()
        {

        }

        public void End()
        {
            // 删除所有.lua.bytes文件
            //ToLuaMenu.ClearLuaBytesFiles(LuaSettings.toluaLuaDir);
            //ToLuaMenu.ClearLuaBytesFiles(LuaSettings.luaDir);
            AssetDatabase.Refresh();

            // 开启perforce插件
            //P4Connect.Config.PerforceEnabled = true;

            EditorUtility.ClearProgressBar();
        }
    }
}