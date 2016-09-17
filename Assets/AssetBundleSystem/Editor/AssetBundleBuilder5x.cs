using UnityEngine;
using UnityEditor;

#if UNITY_5
namespace AssetBundleSystem
{
    public class AssetBundleBuilder5x : AssetBundleBuilder
    {
        public AssetBundleBuilder5x(AssetBundlePathResolver pathResolver) : base(pathResolver) 
        {}
    }
}
#endif