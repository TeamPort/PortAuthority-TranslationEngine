using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Web.Script.Serialization;

public class Program
{
    private class Parameters
    {
        public FileInfo info;
        public bool synthetic;
    }

    private static Data gBinary;
    private static Data gSynthetic;

    public static void Main(String[] Args)
    {
        FileInfo x86 = new FileInfo(Args[0]);
        FileInfo aarch64 = new FileInfo(Args[1]);

        Parameters parameters = new Parameters();
        parameters.info = x86;
        parameters.synthetic = true;

        Parameters parameters2 = new Parameters();
        parameters2.info = aarch64;
        parameters2.synthetic = false;

        Thread thread = new Thread(ReadData);
        Thread thread2 = new Thread(ReadData);
        thread.Start(parameters);
        thread2.Start(parameters2);
        thread.Join();
        thread2.Join();

        Synthetic.buildSyntheticInstanceMap(gSynthetic, 2);
    }

    private static void ReadData(object data)
    {
        Parameters parameters = (Parameters)data;
        String path = Decompress(parameters.info);
        var Deserializer = new JavaScriptSerializer();
        Deserializer.MaxJsonLength = int.MaxValue;
        if(parameters.synthetic)
        {
            gSynthetic = Deserializer.Deserialize<Data>(File.ReadAllText(path));
        }
        else
        {
            gBinary = Deserializer.Deserialize<Data>(File.ReadAllText(path));
        }
        File.Delete(path);
    }

    private static String Decompress(FileInfo replay)
    {
        using(FileStream stream = replay.OpenRead())
        {
            string temp = replay.FullName.Remove(replay.FullName.Length - replay.Extension.Length);
            using(FileStream decompressedStream = File.Create(temp))
            {
                using(GZipStream zipStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    zipStream.CopyTo(decompressedStream);
                }
            }

            return temp;
        }
    }
}