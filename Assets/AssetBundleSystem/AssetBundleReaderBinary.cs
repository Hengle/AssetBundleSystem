using System;
using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleReaderBinary : AssetBundleReader
    {
        public override void Read(Stream fs)
        {
            if (fs.Length < 4)
                return;

            BinaryReader br = new BinaryReader(fs);
            char[] fileHeadChars = br.ReadChars(4);
            //读取文件头判断文件类型，ABDB 即 Asset Bundle Data Binary
            if (fileHeadChars[0] != 'A' || fileHeadChars[1] != 'B' || fileHeadChars[2] != 'D' || fileHeadChars[3] != 'B')
                return;

            version = new Version(br.ReadString());

            int namesCount = br.ReadInt32();
            string[] names = new string[namesCount];
            for (int ii = 0; ii < namesCount; ++ii)
            {
                names[ii] = br.ReadString();
            }

            while (true)
            {
                if (fs.Position == fs.Length)
                    break;

                string name = names[br.ReadInt32()];
                string fileShortName = br.ReadString();
                string hash = br.ReadString();
                long size = br.ReadInt64();
                int dependsCount = br.ReadInt32();
                string[] depends = new string[dependsCount];

                for (int ii = 0; ii < dependsCount; ++ii)
                {
                    depends[ii] = names[br.ReadInt32()];
                }

                AssetBundleData info = new AssetBundleData();
                info.fullName = name;
                info.shortName = fileShortName;
                info.hash = hash;
                info.size = size;
                info.dependencies = depends;
                infoMap[name] = info;
            }

            br.Close();
        }
    }
}