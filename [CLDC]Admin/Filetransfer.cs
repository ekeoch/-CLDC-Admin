using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.IO.Compression;

namespace _CLDC_Admin
{
    class Transfer:Form1
    {
        #region CONSTRUCTOR
        public Transfer()
        {

        }
        #endregion
        public string FileName_;
        /// <summary>
        /// Algorithm to Compress file types
        /// </summary>
        /// <param name="fi"></param>
        public static void Compress(FileInfo fi)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Prevent compressing hidden and 
                // already compressed files.
                if ((File.GetAttributes(fi.FullName)
                    & FileAttributes.Hidden)
                    != FileAttributes.Hidden & fi.Extension != ".gz")
                {
                    // Create the compressed file.
                    using (FileStream outFile = File.Create(fi.FullName + ".gz"))
                    {
                        File.SetAttributes(fi.FullName + ".gz", FileAttributes.Hidden);
                        using (GZipStream Compress =
                            new GZipStream(outFile,
                            CompressionMode.Compress))
                        {
                            // Copy the source file into 
                            // the compression stream.
                            inFile.CopyTo(Compress);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Algorithm to decpompress file types
        /// </summary>
        /// <param name="fi"></param>
        public static void Decompress(FileInfo fi)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Get original file extension, for example
                // "doc" from report.doc.gz.
                string curFile = fi.FullName;
                string origName = curFile.Remove(curFile.Length -
                        fi.Extension.Length);

                //Create the decompressed file.
                using (FileStream outFile = File.Create(origName))
                {
                    using (GZipStream Decompress = new GZipStream(inFile,
                            CompressionMode.Decompress))
                    {
                        // Copy the decompression stream 
                        // into the output file.
                        Decompress.CopyTo(outFile);
                    }
                }
            }
        }

         /// <summary>
         /// This method simply sends the file over IP 
         /// assuming the server/ reciepnt side is already running
         /// </summary>
         /// <param name="IP"></param>
         /// <param name="FilePath"></param>
         /// <returns></returns>
        public string SendFile(string IP, string FilePath)
        {
            try
            {
                FileInfo Fileinfo_ = new FileInfo(FilePath);
                if (Fileinfo_.Length > 26214400)
                {
                    return "4Error! File size is greater than 25mb. . .";
                }
                Compress(Fileinfo_);
                
                if(!FilePath.EndsWith(".gz"))
                {
                FilePath = FilePath + ".gz";
                }
                string splitter = "'\\'";
                char[] delimiter = splitter.ToCharArray();         
                string[] split = null;
                byte[] clientData;
                split = FilePath.Split(delimiter);
                int limit = split.Length;
                string FileName = split[limit - 1].ToString();
                FileName_ = FileName.Remove(FileName.Length - 3, 3);

                Socket clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                byte[] fileName = Encoding.UTF8.GetBytes(FileName); 
                byte[] fileData = File.ReadAllBytes(FilePath); 
                byte[] fileNameLen = BitConverter.GetBytes(fileName.Length); 
                clientData = new byte[4 + fileName.Length + fileData.Length];

                fileNameLen.CopyTo(clientData, 0);
                fileName.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileName.Length);
                clientSock.Connect(IP, 20501); 
                clientSock.Send(clientData);
                clientSock.Close();
                File.Delete(Fileinfo_.FullName + ".gz");//delete the file after compression and send process
                return null;
            }
            catch (Exception ex)
            {
                return "4" + ex.Message;
            }
        }
    }
}

