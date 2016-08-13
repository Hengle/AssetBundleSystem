using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleDataWriter
    {
        public void Save(string path, AssetBundleTarget[] targets)
        {
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            Save(fs, targets);
        }

        protected virtual void Save(Stream stream, AssetBundleTarget[] targets)
        { }
    }
}