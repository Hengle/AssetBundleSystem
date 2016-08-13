using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleData
    {
        public string shortName;
        public string fullName;
        public string hash;
        public long size;
        public string[] dependencies;
        public bool bAnalyzed;
        public AssetBundleData[] dependList;
    }

    public class AssetBundleReader
    {
        public Dictionary<string, AssetBundleData> infoMap = new Dictionary<string, AssetBundleData>();

        protected Dictionary<string, string> _shortName2FullName = new Dictionary<string, string>();

        public Version version { get; protected set; }

        public virtual void Read(Stream fs)
        { }

        // 分析生成依赖树
        public void Analyze()
        {
            foreach (var info in infoMap)
            {
                Analyze(info.Value);
            }
        }

        private void Analyze(AssetBundleData data)
        {
            if (!data.bAnalyzed)
            {
                data.bAnalyzed = true;
                data.dependList = new AssetBundleData[data.dependencies.Length];
                for (int ii = 0; ii < data.dependencies.Length; ++ii)
                {
                    AssetBundleData parent = GetAssetBundleData(data.dependencies[ii]);
                    data.dependList[ii] = parent;
                    Analyze(parent);
                }
            }
        }

        public AssetBundleData GetAssetBundleData(string fullName)
        {
            if (!string.IsNullOrEmpty(fullName) && infoMap.ContainsKey(fullName))
                return infoMap[fullName];
            return null;
        }
    }
}