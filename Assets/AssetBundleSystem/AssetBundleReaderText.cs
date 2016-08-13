using System;
using System.IO;

namespace AssetBundleSystem
{
    public class AssetBundleReaderText : AssetBundleReader
    {
        public override void Read(Stream fs)
        {
            StreamReader sr = new StreamReader(fs);
            char[] fileHeadChars = new char[6];
            sr.Read(fileHeadChars, 0, fileHeadChars.Length);
            //读取文件头判断文件类型，ABDT 即 Asset Bundle Data Text
            if (fileHeadChars[0] != 'A' || fileHeadChars[1] != 'B' || fileHeadChars[2] != 'D' || fileHeadChars[3] != 'T')
                return;

            version = new Version(sr.ReadLine());

            while (true)
            {
                string name = sr.ReadLine();
                if (string.IsNullOrEmpty(name))
                    break;

                string fileShortName = sr.ReadLine();
                string hash = sr.ReadLine();
                long size = long.Parse(sr.ReadLine());
                int dependsCount = int.Parse(sr.ReadLine());
                string[] depends = new string[dependsCount];

                for (int ii = 0; ii < dependsCount; ++ii)
                {
                    depends[ii] = sr.ReadLine();
                }

                AssetBundleData info = new AssetBundleData();
                info.fullName = name;
                info.shortName = fileShortName;
                info.hash = hash;
                info.size = size;
                info.dependencies = depends;
                infoMap[name] = info;
            }

            sr.Close();
        }
    }
}