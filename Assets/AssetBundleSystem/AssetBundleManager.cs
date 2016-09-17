using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AssetBundleSystem
{
    public class AssetBundleManager : MonoBehaviour
    {
        public static Version version = new Version(0, 1, 0, 0);

        public Action onPreInited;
        public Action onInited;

        private AssetBundlePathResolver _pathResolver;
        private AssetBundleReader _dependInfoReader;
        private AssetBundleReader _dependInfoReaderNew;

        private Dictionary<string, AssetBundleInfo> _loadedAssetBundles = new Dictionary<string, AssetBundleInfo>();

        private AssetBundleInfo _curLevelBundleInfo;

        private void Awake()
        {
            _pathResolver = new AssetBundlePathResolver();
            InvokeRepeating("UnloadUnusedBundles", 0, 5f);
        }

        public void PreInit()
        {
            StartCoroutine(CheckBundleInfo());
        }

        public void Init()
        {
            StartCoroutine(CheckUpdate());
        }

        private IEnumerator CheckBundleInfo()
        {
            // 第一次启动游戏时会先将bundle.info文件从StreamingAssets目录复制到PersistentData目录
            if (!File.Exists(Path.Combine(_pathResolver.bundleDir, _pathResolver.dependInfoFileName)))
            {
                Debug.Log("Extracting bundle.info...");
                string bundleCachePath = string.Format("{0}/{1}", _pathResolver.bundleCacheDir, _pathResolver.dependInfoFileName);
                string bundlePath = string.Format("{0}/{1}", _pathResolver.bundleDir, _pathResolver.dependInfoFileName);
                if (Application.platform == RuntimePlatform.Android)
                {
                    WWW www = new WWW(bundleCachePath);
                    yield return www;
                    if (!string.IsNullOrEmpty(www.error))
                    {
                        Debug.LogError(www.error);
                        www.Dispose();
                        yield break;
                    }
                    www.Dispose();

                    // 写入PersistentData目录
                    File.WriteAllBytes(bundlePath, www.bytes);
                    yield return null;
                }
                else
                {
                    File.Copy(bundleCachePath, bundlePath);
                }
            }
            
            // 解析本地的bundle.info文件
            LoadDependInfo(out _dependInfoReader, _pathResolver.dependInfoFileName);

            if (onPreInited != null) onPreInited();
        }

        private IEnumerator CheckUpdate()
        {
            if (!Directory.Exists(_pathResolver.bundleDir))
                Directory.CreateDirectory(_pathResolver.bundleDir);

            // 下载bundle.info文件至
            Debug.Log("Download bundle.info...");
            WWW www = new WWW(string.Format("{0}/{1}", _pathResolver.bundleURL, _pathResolver.dependInfoFileName));
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError(www.error);
                www.Dispose();
                //TODO:下载失败处理
                yield break;
            }

            // 写入persist目录并命名为bundle.info.new
            File.WriteAllBytes(string.Format("{0}/{1}", _pathResolver.bundleDir, _pathResolver.dependInfoFileNameNew), www.bytes);
            www.Dispose();
            yield return null;

            // 下载完成，解析bundle.info.new文件
            LoadDependInfo(out _dependInfoReaderNew, _pathResolver.dependInfoFileNameNew);

            // 比较版本号，当前是最新版本的话跳过更新
            if (_dependInfoReader.version >= _dependInfoReaderNew.version)
            {
                File.Delete(Path.Combine(_pathResolver.bundleDir, _pathResolver.dependInfoFileNameNew));
                
                if (onInited != null) onInited();
                //Util.EventDispatcher.TriggerEvent(Global.UpdateFinish);

                yield break;
            }

            // 删除最新版已经删除但是persist下还存在的ab
            foreach (var info in _dependInfoReader.infoMap)
            {
                if (!_dependInfoReaderNew.infoMap.ContainsKey(info.Key))
                    File.Delete(string.Format("{0}/{1}.ab", _pathResolver.bundleDir, info.Key));
            }

            // 统计需要更新的文件及大小
            long needUpdateSize = 0;
            Dictionary<string, long> needUpdateABs = new Dictionary<string, long>();
            foreach (var info in _dependInfoReaderNew.infoMap)
            {
                string abName = info.Key;
                AssetBundleData abData = info.Value;
                if (!_dependInfoReader.infoMap.ContainsKey(abName) || _dependInfoReader.infoMap[abName].hash != abData.hash)
                {
                    needUpdateABs.Add(abName, abData.size);
                    needUpdateSize += abData.size;
                }
            }

            // 更新
            long updatedSize = 0;
            foreach (var ab in needUpdateABs)
            {
                string abName = ab.Key;
                Debug.Log(string.Format("Update {0}...", abName));

                www = new WWW(Path.Combine(_pathResolver.bundleURL, abName));
                yield return www;

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.error);
                    www.Dispose();
                    continue;
                }

                string abPath = Path.Combine(_pathResolver.bundleDir, abName);
                File.WriteAllBytes(abPath, www.bytes);
                www.Dispose();

                // 解压
                string decompressPath = string.Format("{0}.tmp", abPath);
                LZMAUtil.DecompressFile(abPath, decompressPath);
                File.Delete(abPath);
                yield return null;
                File.Move(decompressPath, abPath);

                updatedSize += ab.Value;
            }

            // 覆盖bundle.info文件
            string dependInfoFilePath = Path.Combine(_pathResolver.bundleDir, _pathResolver.dependInfoFileName);
            string newDependInfoFilePath = Path.Combine(_pathResolver.bundleDir, _pathResolver.dependInfoFileNameNew);
            File.Delete(dependInfoFilePath);
            File.Move(newDependInfoFilePath, dependInfoFilePath);

            _dependInfoReader = _dependInfoReaderNew;
            _dependInfoReaderNew = null;

            // 初始化完成
            if (onInited != null) onInited();
        }

        private void LoadDependInfo(out AssetBundleReader reader, string fileName)
        {
            reader = null;
            string depFilePath = Path.Combine(_pathResolver.bundleDir, fileName);
            FileStream fs = new FileStream(depFilePath, FileMode.Open, FileAccess.Read);
            if (fs.Length > 4)
            {
                BinaryReader br = new BinaryReader(fs);
                if (br.ReadChar() == 'A' && br.ReadChar() == 'B' && br.ReadChar() == 'D')
                {
                    if (br.ReadChar() == 'T')
                        reader = new AssetBundleReaderText();
                    else
                        reader = new AssetBundleReaderBinary();

                    fs.Position = 0;
                    reader.Read(fs);
                }
            }
            fs.Close();
        }

        public UnityObject Load(string path, Type type)
        {
            AssetBundleInfo abi = GetBundleInfo(path);
            return abi.Load(Path.GetFileName(path), type);
        }

        public GameObject LoadAndInstantiate(string path)
        {
            AssetBundleInfo abi = GetBundleInfo(path);
            return abi.LoadAndInstantiate(Path.GetFileName(path));
        }

        public GameObject LoadAndInstantiate(string path, Vector3 position, Quaternion rotation)
        {
            AssetBundleInfo abi = GetBundleInfo(path);
            return abi.LoadAndInstantiate(Path.GetFileName(path), position, rotation);
        }

        public void LoadLevel(string path)
        {
            if (_curLevelBundleInfo != null)
                _curLevelBundleInfo.ReduceRefCount();
            AssetBundleInfo abi = GetBundleInfo(path, true);
            abi.AddRefCount();
        }

        private AssetBundleInfo GetBundleInfo(string path, bool isLevel = false)
        {
            AssetBundleData abd = null;

            // 获取该资源对应的assetbundle名字
            string fullPath = isLevel ? string.Format("Assets/Art/Scenes/{0}/{1}", path, path) : string.Format("Assets/Resources/{0}", path);
            string bundleName = null;
            do
            {
                bundleName = HashUtil.Get(AssetBundleUtils.ConvertToABName(fullPath));
                abd = _dependInfoReader.GetAssetBundleData(bundleName);
                if (abd != null)
                    break;
                fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/'));
            } while (fullPath.Contains("/") && !isLevel);

            if (abd == null)
            {
                Debug.LogError(string.Format("Failed to load assetbundle step1: {0}", path));
                return null;
            }

            return LoadAssetBundle(abd);
        }

        private AssetBundleInfo LoadAssetBundle(AssetBundleData abd)
        {
            // 如果此assetbundle已经加载了，那么它及其父bundle都不用加载了
            if (_loadedAssetBundles.ContainsKey(abd.fullName))
                return _loadedAssetBundles[abd.fullName];

            List<AssetBundleInfo> parents = new List<AssetBundleInfo>();
            for (int ii = 0; ii < abd.dependencies.Length; ++ii)
            {
                AssetBundleInfo parent = LoadAssetBundle(_dependInfoReader.GetAssetBundleData(abd.dependencies[ii]));
                parents.Add(parent);
            }

            // 同步加载AssetBundle
            string abPath = string.Format("{0}/{1}.ab", _pathResolver.bundleDir, abd.fullName);
            string abCachePath = string.Format("{0}/{1}.ab", _pathResolver.bundleCacheDirSync, abd.fullName);
            // 优先读取persist目录，若不存在则读取streamingAssets目录
            string bundlePath = File.Exists(abPath) ? abPath : abCachePath;

#if UNITY_5
            AssetBundle ab = AssetBundle.LoadFromFile(bundlePath);
#else
            AssetBundle ab = AssetBundle.CreateFromFile(bundlePath);
#endif
            if (ab == null)
            {
                Debug.LogError(string.Format("Failed to load assetbundle step2: {0}", bundlePath));
                return null;
            }
            _loadedAssetBundles[abd.fullName] = new AssetBundleInfo(abd, ab);

            // 添加索引
            AssetBundleInfo abi = _loadedAssetBundles[abd.fullName];
            for (int ii = 0; ii < parents.Count; ++ii)
            {
                parents[ii].AddReference(abi.bundle);
            }

            return _loadedAssetBundles[abd.fullName];
        }

        // 卸载不再被引用的assetbundle
        private void UnloadUnusedBundles()
        {
            List<string> unusedList = new List<string>();
            foreach (var bundle in _loadedAssetBundles)
            {
                if (bundle.Value.isUnused)
                {
                    bundle.Value.Dispose();
                    unusedList.Add(bundle.Key);
                }
            }

            unusedList.ForEach(n => _loadedAssetBundles.Remove(n));
        }
    }
}