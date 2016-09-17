using UnityEngine;
using UnityEditor;

#if !UNITY_5
namespace AssetBundleSystem
{
    public class AssetBundleBuilder4x : AssetBundleBuilder
    {
        public AssetBundleBuilder4x(AssetBundlePathResolver pathResolver) : base(pathResolver) 
        {}
    }
}
#endif