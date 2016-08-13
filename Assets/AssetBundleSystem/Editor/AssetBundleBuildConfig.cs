using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleSystem
{
    public class AssetBundleBuildConfig : ScriptableObject
    {
        public enum Format
        {
            Text, Binary
        }

        public string bundlePath;
        public Format bundleInfoFileFormt = Format.Binary;

        public List<AssetBundleConfigNode> allDependNodes = new List<AssetBundleConfigNode>();
        public List<string> persistentAssets = new List<string>();

        private void OnEnable()
        {
            allDependNodes.ForEach(n => n.filters.ForEach(f => f.isFolder = !File.Exists(AssetBundleUtils.ConvertToAbsolutePath(n.dir) + "/" + f.path)));
        }

        public AssetBundleConfigNode AddNode(int level)
        {
            int newID = allDependNodes.Count > 0 ? allDependNodes[allDependNodes.Count - 1].ID + 1 : 1;
            AssetBundleConfigNode newNode = new AssetBundleConfigNode(newID, level);
            allDependNodes.Add(newNode);
            return newNode;
        }

        public AssetBundleConfigNode InsertNode(int idx, int level)
        {
            int newID = allDependNodes.Count > 0 ? allDependNodes[allDependNodes.Count - 1].ID + 1 : 1;
            AssetBundleConfigNode newNode = new AssetBundleConfigNode(newID, level);
            allDependNodes.Insert(idx, newNode);
            return newNode;
        }

        public void RemoveNode(AssetBundleConfigNode node)
        {
            int idx = allDependNodes.IndexOf(node);
            for (int ii = idx + 1; ii < allDependNodes.Count; ++ii)
            {
                if (allDependNodes[ii].level <= node.level)
                    break;
                --allDependNodes[ii].level;
            }
            allDependNodes.RemoveAt(idx);
        }

        public void PopNode(AssetBundleConfigNode node, bool bPop)
        {
            int idx = allDependNodes.IndexOf(node);
            int swapIdx = idx + (bPop ? -1 : 1);
            if (swapIdx < 0 || swapIdx >= allDependNodes.Count)
                return;

            AssetBundleConfigNode swapNode = allDependNodes[swapIdx];
            allDependNodes[swapIdx] = node;
            allDependNodes[idx] = swapNode;
        }
    }

    public enum ExportType
    {
        Solo, Whole
    }

    [System.Serializable]
    public class AssetBundleConfigNode
    {
        public bool enabled = true;
        public bool isIndependent = false;
        public int ID;
        public int level;
        public string name;
        public string dir;
        public ExportType exportType = ExportType.Whole;
        public List<AssetBundleFilter> filters = new List<AssetBundleFilter>();

        public bool needDelete { get; set; }

        public AssetBundleConfigNode(int ID, int level)
        {
            this.ID = ID;
            this.level = level;
            name = string.Format("Node{0:D3}", ID);
            needDelete = false;
        }
    }

    [System.Serializable]
    public class AssetBundleFilter
    {
        public bool valid = true;
        public string path = string.Empty;
        public string filter = "*.*";
        public SearchOption searchOption = SearchOption.AllDirectories;

        public bool isFolder { get; set; }

        public AssetBundleFilter(bool isFolder)
        {
            this.isFolder = isFolder;
        }
    }
}