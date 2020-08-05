using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveConverter
{
    class Program
    {
        static void Main(string[] args)
        {

            string path;
            if (args.Length != 0)
            {
                path = args[0];


                if (Path.GetExtension(path).Contains("save"))
                {

                    SaveUtil.DecompressToDirectory(path, "Extracted");


                }
            }
            else
            {
                SaveUtil.CompressDirectory("Extracted","Compressed.save");
              
            }

          
        }

        partial class SaveUtil
        {
         
          
            public static void CompressFile(string sDir, string sRelativePath, GZipStream zipStream)
            {
                //Compress file name
                char[] chars = sRelativePath.ToCharArray();
                zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
                foreach (char c in chars)
                    zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));

                //Compress file content
                byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
                zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
                zipStream.Write(bytes, 0, bytes.Length);
            }

            public static void CompressDirectory(string sInDir, string sOutFile)
            {
                IEnumerable<string> sFiles = Directory.GetFiles(sInDir, "*.*", System.IO.SearchOption.AllDirectories);
                int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar
                    ? sInDir.Length
                    : sInDir.Length + 1;

                using (FileStream outFile = File.Open(sOutFile, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
                    foreach (string sFilePath in sFiles)
                    {
                        string sRelativePath = sFilePath.Substring(iDirLen);
                       
                        CompressFile(sInDir, sRelativePath, str);
                    }
            }

            public static bool DecompressFile(string sDir, GZipStream zipStream)
            {
                //Decompress file name
                byte[] bytes = new byte[sizeof(int)];
                int Readed = zipStream.Read(bytes, 0, sizeof(int));
                if (Readed < sizeof(int))
                    return false;

                int iNameLen = BitConverter.ToInt32(bytes, 0);
                if (iNameLen > 255)
                {
                    throw new Exception("Failed to decompress \"" + sDir +
                                        "\" (file name length > 255). The file may be corrupted.");
                }

                bytes = new byte[sizeof(char)];
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < iNameLen; i++)
                {
                    zipStream.Read(bytes, 0, sizeof(char));
                    char c = BitConverter.ToChar(bytes, 0);
                    sb.Append(c);
                }

                string sFileName = sb.ToString();
              

                //Decompress file content
                bytes = new byte[sizeof(int)];
                zipStream.Read(bytes, 0, sizeof(int));
                int iFileLen = BitConverter.ToInt32(bytes, 0);

                bytes = new byte[iFileLen];
                zipStream.Read(bytes, 0, bytes.Length);

                string sFilePath = Path.Combine(sDir, sFileName);
                string sFinalDir = Path.GetDirectoryName(sFilePath);
                if (!Directory.Exists(sFinalDir))
                    Directory.CreateDirectory(sFinalDir);

                int maxRetries = 4;
                for (int i = 0; i <= maxRetries; i++)
                {
                    try
                    {
                        using (FileStream outFile = File.Open(sFilePath, System.IO.FileMode.Create,
                            System.IO.FileAccess.Write))
                        {
                            outFile.Write(bytes, 0, iFileLen);
                        }

                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        if (i >= maxRetries || !File.Exists(sFilePath))
                        {
                            throw;
                        }

                        
                        Thread.Sleep(250);
                    }
                }

                return true;
            }

            public static void DecompressToDirectory(string sCompressedFile, string sDir)
            {
                int maxRetries = 4;
                for (int i = 0; i <= maxRetries; i++)
                {
                    try
                    {
                        using (FileStream inFile = File.Open(sCompressedFile, System.IO.FileMode.Open,
                            System.IO.FileAccess.Read))
                        using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
                            while (DecompressFile(sDir, zipStream))
                            {
                            }

                        ;

                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        if (i >= maxRetries || !File.Exists(sCompressedFile))
                        {
                            throw;
                        }

                          
                        Thread.Sleep(250);
                    }
                }
            }

           
        }
    }
}
