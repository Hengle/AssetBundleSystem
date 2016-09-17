using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AssetBundleSystem
{
    public class AssetBundleInfo
    {
        // 延迟销毁时间，避免刚加载玩就释放，或者刚释放完又加载
        private const float MINLIFETIME = 5f;

        private float _readyTime;
        private int _refCount; // 主要针对关卡bundle
        private List<WeakReference> _references = new List<WeakReference>();

        public string name { get; private set; }
        public AssetBundle bundle { get; private set; }
        public AssetBundleData data { get; private set; }
        
        public bool isUnused 
        { 
            get 
            {
                UpdateReferences();
                return _references.Count == 0 && _refCount == 0 && Time.time > _readyTime + MINLIFETIME; 
            } 
        }

        public AssetBundleInfo(AssetBundleData data, AssetBundle bundle)
        {
            this.name = data.fullName;
            this.data = data;
            this.bundle = bundle;
        }

        public UnityObject Load(string objName, Type type)
        {
#if UNITY_5
            UnityObject origin = bundle.LoadAsset(objName, type);
#else
            UnityObject origin = bundle.Load(objName, type);
#endif
            AddReference(origin);
            return origin;
        }

        public GameObject LoadAndInstantiate(string objName)
        {
#if UNITY_5
            UnityObject prefab = bundle.LoadAsset<GameObject>(objName);
#else
            UnityObject prefab = bundle.Load(objName, typeof(GameObject));
#endif
            if (prefab == null)
            {
                Debug.LogError(string.Format("Failed to load {0} from {1}", objName, name));
                return null;
            }

            GameObject inst = UnityObject.Instantiate(prefab) as GameObject;
            AddReference(inst);
            return inst;
        }

        public GameObject LoadAndInstantiate(string objName, Vector3 pos, Quaternion rot)
        {
#if UNITY_5
            UnityObject prefab = bundle.LoadAsset<GameObject>(objName);
#else
            UnityObject prefab = bundle.Load(objName, typeof(GameObject));
#endif
            if (prefab == null)
            {
                Debug.LogError(string.Format("Failed to load {0} from {1}", objName, name));
                return null;
            }

            GameObject inst = UnityObject.Instantiate(prefab, pos, rot) as GameObject;
            AddReference(inst);
            return inst;
        }

        public void AddRefCount()
        {
            ++_refCount;
        }

        public void ReduceRefCount()
        {
            --_refCount;
        }

        public void AddReference(UnityObject obj)
        {
            _readyTime = Time.time;

            int idx = _references.FindIndex(r => obj.Equals(r.Target));
            if (idx >= 0)
                return;

            WeakReference wr = new WeakReference(obj);
            _references.Add(wr);
        }

        private void UpdateReferences()
        {
            for (int ii = 0; ii < _references.Count;)
            {
                UnityObject obj = _references[ii].Target as UnityObject;
                if (obj == null)
                    _references.RemoveAt(ii);
                else
                    ++ii;
            }
        }

        public void Dispose()
        {
            bundle.Unload(true);
        }
    }
}