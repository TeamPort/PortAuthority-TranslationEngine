using System;
using System.IO;
using System.IO.Compression;
using System.Web.Script.Serialization;

public class Program
{
    public class Instruction
    {
        public UInt64 a;
        public Int32 o;
        public String m;
    }

    public class Data
    {
        public String triple;
        public Int32 size;
        public Instruction[] run;
    }

    public static void Main(String[] Args)
    {
        FileInfo info = new FileInfo(Args[0]);
        String path = Decompress(info);

        var Deserializer = new JavaScriptSerializer();
        Deserializer.MaxJsonLength = int.MaxValue;
        Data run = Deserializer.Deserialize<Data>(File.ReadAllText(path));
        Console.WriteLine(run.triple);
        File.Delete(path);
    }

    public static String Decompress(FileInfo replay)
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