using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleSystem
{
#if UNITY_EDITOR
    public class AssetCacheInfo
    {
        // 源文件的hash，比较变化
        public string fileHash;
        // 源文件meta文件的hash，部分类型的素材需要结合这个来判断变化
        public string metaHash;
        // 上次打好的AB的CRC值，用于增量判断
        public string bundleCRC;
        // 所依赖的那些文件
        public string[] dependNames;
    }
#endif

    public class AssetBundleUtils
    {
#if UNITY_EDITOR
        public static AssetBundlePathResolver pathResolver;
        public static DirectoryInfo assetDir = new DirectoryInfo(Application.dataPath);
        public static string assetPath = assetDir.FullName;
        public static DirectoryInfo projectDir = assetDir.Parent;
        public static string projectPath = projectDir.FullName;

        static Dictionary<string, string> _fileHashCache;
        static Dictionary<string, AssetCacheInfo> _fileHashOld;

        public static void Init()
        {
            _fileHashCache = new Dictionary<string, string>();
            _fileHashOld = new Dictionary<string, AssetCacheInfo>();
            LoadCache();
        }

        public static void LoadCache()
        {
            string cacheTextFilePath = pathResolver.cacheFilePath;
            if (File.Exists(cacheTextFilePath))
            {
                string value = File.ReadAllText(cacheTextFilePath);
                StringReader sr = new StringReader(value);

                // 版本比较
                string vString = sr.ReadLine();
                bool bWrongVer = false;
                try
                {
                    Version ver = new Version(vString);
                    bWrongVer = ver.Minor < AssetBundleManager.version.Minor || ver.Major < AssetBundleManager.version.Major;
                }
                catch (Exception)
                {
                    bWrongVer = true;
                }

                if (bWrongVer)
                    return;

                while (true)
                {
                    string path = sr.ReadLine();
                    if (path == null)
                        break;

                    AssetCacheInfo cache = new AssetCacheInfo();
                    cache.fileHash = sr.ReadLine();
                    cache.metaHash = sr.ReadLine();
                    cache.bundleCRC = sr.ReadLine();
                    int dependsCount = int.Parse(sr.ReadLine());
                    cache.dependNames = new string[dependsCount];
                    for (int ii = 0; ii < dependsCount; ++ii)
                    {
                        cache.dependNames[ii] = sr.ReadLine();
                    }
                    _fileHashOld[path] = cache;
                }
            }
        }

        public static string GetFileHash(string path, bool force = false)
        {
            string _hexStr = null;
            if (_fileHashCache.ContainsKey(path) && !force)
            {
                _hexStr = _fileHashCache[path];
            }
            else if (!File.Exists(path))
            {
                _hexStr = "FileNotExists";
            }
            else
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                _hexStr = HashUtil.Get(fs);
                _fileHashCache[path] = _hexStr;
                fs.Close();
            }

            return _hexStr;
        }

        public static string ConvertToAbsolutePath(string path)
        {
            if (!path.StartsWith("Assets"))
                return path;

            return Application.dataPath + path.Substring(6);
        }

        public static string ConvertToAssetPath(string path)
        {
            if (path.StartsWith("Assets"))
                return path;

            return path.Replace(Application.dataPath, "Assets").Replace("\\", "/");
        }
#endif

        public static string ConvertToABName(string path)
        {
            return path.Replace('\\', '.').Replace('/', '.').Replace(" ", "_").ToLower();
        }
    }
}