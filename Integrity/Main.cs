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

    private static Data gBase;
    private static Data gPortedBinary;

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

        Int32 count = 0;
        const Int32 n = 3;
        Data synthetic = Synthetic.buildSyntheticInstanceMap(gBase, n);
        Int32 size = Math.Min(synthetic.run.Length, gPortedBinary.run.Length);
        for(Int32 current = 0; current < size-(n-1); current+=n)
        {
            Int32 m = n;
            bool test = true;
            while(m > 0)
            {
                test = test && gPortedBinary.run[current].m.ToUpper() == synthetic.run[current].m.ToUpper();
                m--;
            }
            if(test)
            {
                count++;
            }
        }

        Console.WriteLine(count);
        Console.WriteLine(size/n);
    }

    private static void ReadData(object data)
    {
        Parameters parameters = (Parameters)data;
        String path = Decompress(parameters.info);
        var Deserializer = new JavaScriptSerializer();
        Deserializer.MaxJsonLength = int.MaxValue;
        if(parameters.synthetic)
        {
            gBase = Deserializer.Deserialize<Data>(File.ReadAllText(path));
        }
        else
        {
            gPortedBinary = Deserializer.Deserialize<Data>(File.ReadAllText(path));
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