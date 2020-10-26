using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.VisualBasic.Devices;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace microsoft.botsay
{
    class Program
    {
        public static string directoryPath = @".";
        public static int Main(string[] argv)
        {
            if (argv.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                        .InformationalVersion
                                        .ToString();

                Console.WriteLine($"GZipTest.exe v{versionString}");
                Console.WriteLine("-------------");
                Console.WriteLine("Usage: GZipTest.exe compress <file_to_compress> <directory_to_save> \n       GZipTest.exe decompress <directory_to_decompress> <directory_to_save> ");
                return 1;
            }

            if (argv.Length != 3)
            {
                Console.WriteLine("Usage: GZipTest.exe <compress/decompress> <in_dir compressed_file> | <compressed_file out_dir>");
                return 1;
            }

            string sDir;
            string sCompressedFile;
            string sCommand = argv[0];

            try
            {
                if (sCommand == "compress")
                {
                    sDir = argv[1];
                    sCompressedFile = argv[2];
                    SplitFile(directoryPath + @"\" + sDir, Convert.ToInt32(2), sCompressedFile);
                    MergeFile(sCompressedFile);
                    Directory.Delete(directoryPath + @"\" + sCompressedFile);
                    Console.WriteLine("Wait for compression to finish ... ");
                }
                else if (sCommand == "decompress")
                {
                    sCompressedFile = argv[1];
                    sDir = argv[2];     
                    FileInfo directorySelected = new FileInfo(directoryPath + @"\" + sCompressedFile);
                    Decompress(directorySelected, sDir);  
                }
                else
                {
                    Console.Error.WriteLine("Wrong arguments");
                    return 1;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
        public static bool MergeFile(string inputfoldername1)
        {
            bool Output = false;
            try
            {
                DirectoryInfo tmpfiles = new DirectoryInfo(directoryPath + @"\" + inputfoldername1);
                FileStream outPutFile = null;
                string PrevFileName = "";
                foreach (FileInfo tempFile in tmpfiles.GetFiles("*.gz"))
                {
                    string fileName = tempFile.FullName;
                    string file_extension = Path.GetExtension(Path.GetFileNameWithoutExtension(tempFile.Name));
                    string baseFileName = inputfoldername1 + file_extension ;
                    string extension = tempFile.Extension;
                    if (!PrevFileName.Equals(baseFileName))
                    {
                        if (outPutFile != null)
                        {
                            outPutFile.Flush();
                            outPutFile.Close();
                        }
                        
                        outPutFile = new FileStream(directoryPath + @"\" + inputfoldername1 + file_extension+ extension, FileMode.OpenOrCreate, FileAccess.Write);

                    }

                    int bytesRead = 0;
                    byte[] buffer = new byte[1024];
                    FileStream inputTempFile = new FileStream(tempFile.FullName, FileMode.OpenOrCreate, FileAccess.Read);

                    while ((bytesRead = inputTempFile.Read(buffer, 0, 1024)) > 0)
                        outPutFile.Write(buffer, 0, bytesRead);

                    inputTempFile.Close();
                    File.Delete(tempFile.FullName);
                    PrevFileName = baseFileName;

                }

                outPutFile.Close();
            }
            catch
            {
                throw new ArgumentException();
            }

            return Output;

        }
        public static void Decompress(FileInfo fileToDecompress, string newname)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string file_extension = Path.GetExtension(Path.GetFileNameWithoutExtension(fileToDecompress.Name));
                string newFileName = newname;

                using (FileStream decompressedFileStream = File.Create(newFileName + file_extension))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
        }
        
        public static void Compress(string outfilename, string  SourceFile, Stream bytestream, int i  )
        {
            using (FileStream compressedFileStream = File.Create(directoryPath + @"\" + outfilename + @"\" + i + SourceFile.Substring(2) + ".gz"))
            {
                using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                   CompressionMode.Compress))
                {
                    bytestream.CopyTo(compressionStream);
                    Console.WriteLine(i + SourceFile.Substring(2) + " is " + compressedFileStream.Length + " bytes");
                }
            }
        }


        public static void SplitFile(string SourceFile, double nNoofFiles, string outfilename)
        {
            int SizeofEachFile;
            DirectoryInfo di = Directory.CreateDirectory(directoryPath + @"\" + outfilename);
            try
            {
                FileStream fs = new FileStream(SourceFile, FileMode.Open, FileAccess.Read);
                double ram = new ComputerInfo().AvailablePhysicalMemory;
                nNoofFiles = (int)Math.Ceiling(Convert.ToDouble(fs.Length) / ram);
                if (nNoofFiles > 1)
                {
                    Console.WriteLine("Due to the lack of RAM file will be split into:{0}", nNoofFiles);
                    SizeofEachFile = (int)Math.Ceiling(fs.Length / nNoofFiles);
                }
                else
                { 
                    SizeofEachFile = (int)fs.Length;
                }

                for (int i = 0; i < nNoofFiles; i++)
                {
                    int bytesRead = 0;
                    byte[] buffer = new byte[SizeofEachFile];
                    if ((bytesRead = fs.Read(buffer, 0, SizeofEachFile)) > 0)
                    {
                        Stream bytestream = new MemoryStream(buffer);
                        Console.WriteLine(i + SourceFile.Substring(2) + " is being compressed ... ");
                        Thread t = new Thread(() => Compress(outfilename, SourceFile, bytestream, i));
                        t.Start();
                    }
                }
                fs.Close();
            }
            catch (Exception Ex)
            {
                throw new ArgumentException(Ex.Message);
            }
        }
    }
}
