using System.Collections.Generic;
using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleDataWriterBinary : AssetBundleDataWriter
    {
        protected override void Save(Stream stream, AssetBundleTarget[] targets)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            //写入文件头判断文件类型用，ABDB 即 Asset Bundle Data Binay
            bw.Write(new char[] { 'A', 'B', 'D', 'B' });
            bw.Write(AssetBundleManager.version.ToString());

            List<string> bundleNames = new List<string>();
            foreach (AssetBundleTarget target in targets)
            {
                bundleNames.Add(target.bundleName);
            }

            // 写入文件名池
            bw.Write(bundleNames.Count);
            foreach (string bundleName in bundleNames)
            {
                bw.Write(bundleName);
            }

            // 写入详细信息
            foreach (AssetBundleTarget target in targets)
            {
                bw.Write(bundleNames.IndexOf(target.bundleName));
                bw.Write(target.bundleShortName);
                bw.Write(target.bundleCRC);
                bw.Write(target.size);
                bw.Write(target.parents.Count);
                foreach (AssetBundleTarget parent in target.parents)
                {
                    bw.Write(bundleNames.IndexOf(parent.bundleName));
                }
            }

            bw.Close();
        }
    }
}