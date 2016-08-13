using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleDataWriterText : AssetBundleDataWriter
    {
        protected override void Save(System.IO.Stream stream, AssetBundleTarget[] targets)
        {
            StreamWriter sw = new StreamWriter(stream);
            //写入头文件判断文件类型用，ABDT 即 Asset Bundle Data Text
            sw.WriteLine("ABDT");
            sw.WriteLine(AssetBundleManager.version.ToString());

            for (int ii = 0; ii < targets.Length; ++ii)
            {
                AssetBundleTarget target = targets[ii];

                sw.WriteLine(target.bundleName);
                sw.WriteLine(target.bundleShortName);
                sw.WriteLine(target.bundleCRC);
                sw.WriteLine(target.size);
                sw.WriteLine(target.parents.Count);
                foreach (AssetBundleTarget parent in target.parents)
                {
                    sw.WriteLine(parent.bundleName);
                }
            }

            sw.Close();
        }
    }
}