using System.IO;
using UnityEngine;

namespace AssetBundleSystem
{
    public class AssetBundlePathResolver
    {
        public string cacheFilePath = "Assets/Scripts/AssetBundleSystem/cache.txt";
        public string dependInfoFileName = "bundle.info";
        public string dependInfoFileNameNew = "bundleNew.info";

        public string bundleURL { get; private set; }
        public string bundleDir { get; private set; }
        public string bundleCacheDir { get; private set; }
        public string bundleCacheDirSync { get; private set; }
        
        public AssetBundlePathResolver()
        {
            bundleDir = Application.persistentDataPath;
            bundleCacheDir = Application.streamingAssetsPath;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    bundleURL = "http://192.168.1.188/down/windows";
                    bundleCacheDirSync = bundleCacheDir;
                    break;
                case RuntimePlatform.Android:
                    bundleURL = "http://192.168.1.188/down/android";
                    bundleCacheDirSync = string.Format("{0}!assets/assetbundles", Application.dataPath);
                    break;
                case RuntimePlatform.IPhonePlayer:
                    bundleURL = "http://192.168.1.188/down/ios";
                    bundleCacheDirSync = bundleCacheDir;
                    break;
            }
        }
    }
}