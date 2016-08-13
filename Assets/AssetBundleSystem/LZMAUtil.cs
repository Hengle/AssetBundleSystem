using System;
using System.IO;
using SevenZip.Compression.LZMA;
using UnityEngine;

namespace AssetBundleSystem
{
    public class LZMAUtil
    {
        public static bool CompressFile(string inFile, string outFile)
        {
            if (!File.Exists(inFile))
            {
                Debug.LogError(string.Format("{0} does not exist!", inFile));
                return false;
            }

            if (File.Exists(outFile))
                File.Delete(outFile);

            Encoder coder = new Encoder();
            FileStream input = new FileStream(inFile, FileMode.Open);
            FileStream output = new FileStream(outFile, FileMode.Create);

            // write the encode properties
            coder.WriteCoderProperties(output);
            
            // write the decompressed file size
            output.Write(BitConverter.GetBytes(input.Length), 0, 8);
            
            // encode the file
            coder.Code(input, output, input.Length, -1, null);

            output.Flush();
            output.Close();
            input.Close();

            return true;
        }

        public static bool DecompressFile(string inFile, string outFile)
        {
            if (!File.Exists(inFile))
            {
                Debug.LogError(string.Format("{0} does not exist!", inFile));
                return false;
            }

            if (File.Exists(outFile))
                File.Delete(outFile);

            Decoder coder = new Decoder();
            FileStream input = new FileStream(inFile, FileMode.Open);
            FileStream output = new FileStream(outFile, FileMode.Create);

            // read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);

            // read the decompressed file size
            byte[] fileLenthBytes = new byte[8];
            input.Read(fileLenthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLenthBytes, 0);

            // decompress the file
            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);

            output.Flush();
            output.Close();
            input.Close();

            return true;
        }
    }
}