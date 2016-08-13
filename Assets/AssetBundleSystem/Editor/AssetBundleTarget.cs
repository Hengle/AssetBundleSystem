using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleSystem
{
    public class AssetBundleNode
    {
        public int ID;
        public int level;
        public bool hasChild;
        public bool isIndependent;
        public List<AssetBundleTarget> targets = new List<AssetBundleTarget>();

        public AssetBundleNode(int ID, int level, bool isIndependent, bool hasChild)
        {
            this.ID = ID;
            this.level = level;
            this.hasChild = hasChild;
            this.isIndependent = isIndependent;
        }
    }

    public class AssetBundleTarget
    {
        public long size;
        public AssetBundleNode node;
        public string mainAsset;
        public List<string> assets;
        public string bundleName;
        public string bundleShortName;
        // 上次打好的AB的CRC值（用于增量打包）
        public string bundleCRC;

        private bool _isFileChanged = false;
        private bool _isAnalyzed = false;
        private bool _isDependTreeChanged = false;
        private AssetCacheInfo _cacheInfo;
        private string _metaHash;

        public List<AssetBundleTarget> parents = new List<AssetBundleTarget>();

        public AssetBundleTarget(string name, string dir, AssetBundleNode node, string main, ref List<string> assets)
        {
            this.node = node;
            this.assets = assets;
            mainAsset = main;
            if (assets.Count == 1)
                mainAsset = assets[0];
            if (string.IsNullOrEmpty(mainAsset))
            {
                // Resources下的Node不允许目录重复，非Resources下的允许，所以此处要做处理防止bundleName重复
                bundleName = HashUtil.Get(AssetBundleUtils.ConvertToABName(dir.Contains("/Resources/") || dir.EndsWith("/Resources") ? dir : dir + node.ID));
                bundleShortName = name.ToLower();
            }
            else
            {
                string pathWithNoExt = mainAsset.Substring(0, mainAsset.LastIndexOf('.'));
                bundleName = HashUtil.Get(AssetBundleUtils.ConvertToABName(pathWithNoExt));
                bundleShortName = Path.GetFileName(mainAsset).ToLower();
            }

            _isFileChanged = true;
            _metaHash = "0";
        }

        // 分析引用关系
        public void Analyze(ref List<AssetBundleNode> allNodes)
        {
            List<string> depends = new List<string>();
            if (!string.IsNullOrEmpty(mainAsset))
            {
                List<string> dependPaths = new List<string>(AssetDatabase.GetDependencies(new string[] { mainAsset }));
                dependPaths.Remove(mainAsset);
                depends.AddRange(dependPaths);
            }
            else
            {
                foreach (string path in assets)
                {
                    List<string> dependPaths = new List<string>(AssetDatabase.GetDependencies(new string[] { path }));
                    dependPaths.Remove(path);
                    depends.AddRange(dependPaths);
                }
                depends = depends.Distinct().ToList();
            }

            for (int ii = allNodes.IndexOf(node); ii >= 0; --ii)
            {
                AssetBundleNode abn = allNodes[ii];
                if (abn.level > node.level)
                    continue;

                foreach (AssetBundleTarget abt in allNodes[ii].targets)
                {
                    foreach (string path in depends)
                    {
                        if (abt.ContainsAsset(path) && !parents.Contains(abt) && abt != this)
                            parents.Add(abt);
                    }
                }
            }
        }

        public bool ContainsAsset(string path)
        {
            if (!string.IsNullOrEmpty(mainAsset))
                return mainAsset == path;
            return assets.Contains(path);
        }

        public void WriteBundle(BuildAssetBundleOptions options, BuildTarget target, string bundleDir)
        {
            string bundleStreamPath = string.Format("{0}/{1}.ab", Application.streamingAssetsPath, bundleName);
            string bundlePath = string.Format("{0}/{1}.ab", bundleDir, bundleName);

            Debug.Log(string.Format("Building {0}", bundleShortName));

            if (node.isIndependent && !node.hasChild)
                BuildPipeline.PushAssetDependencies();

            uint crc = 0;
            if (!string.IsNullOrEmpty(mainAsset))
            {
                if (mainAsset.EndsWith(".unity"))
                    BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { mainAsset }, bundleStreamPath, target, out crc, BuildOptions.UncompressedAssetBundle);
                else
                    BuildPipeline.BuildAssetBundle(AssetDatabase.LoadMainAssetAtPath(mainAsset), null, bundleStreamPath, out crc, options, target);
            }
            else
            {
                List<Object> objs = new List<Object>();
                assets.ForEach(a => objs.Add(AssetDatabase.LoadMainAssetAtPath(a)));
                if (objs.Count == 0)
                {
                    Debug.LogError(string.Format("No assets found for the asset bundle: {0}", bundleShortName));
                    return;
                }
                BuildPipeline.BuildAssetBundle(null, objs.ToArray(), bundleStreamPath, out crc, options, target);
            }

            if (node.isIndependent && !node.hasChild)
                BuildPipeline.PopAssetDependencies();

            bundleCRC = crc.ToString();

            LZMAUtil.CompressFile(bundleStreamPath, bundlePath);
            FileInfo fi = new FileInfo(bundlePath);
            size = fi.Length;
        }

        public void WriteCache(StreamWriter sw)
        {

        }
    }
}