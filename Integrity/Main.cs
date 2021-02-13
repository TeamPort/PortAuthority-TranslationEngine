using System;
using System.Collections.Generic;
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

        Int32 toRet = 0;
        Int32 previous = 0;

        const Int32 n = 2;
        Int32 instances = 0;
        gSynthetic = Synthetic.buildSyntheticInstanceMap(gBase, n);
        float size = Math.Min(gSynthetic.run.Length, gPortedBinary.run.Length);
        for(Int32 current = 0; current < size; current++)
        {
            String mnem = gPortedBinary.run[current].m.ToUpper();
            bool match = mnem == gSynthetic.run[current].m.ToUpper();
            toRet++;
            if(match)
            {
                instances++;
            }

            if(mnem == "RET")
            {
                Int32 offset = 0;
                while((current + offset) < size
                && (current - offset) > 0
                && gSynthetic.run[current + offset].m.ToUpper() != "RET"
                && gSynthetic.run[current - offset].m.ToUpper() != "RET")
                {
                    offset++;
                }

                if((current + offset) >= size)
                {
                    offset = -offset;
                }
                else
                {
                    offset = gSynthetic.run[current + offset].m.ToUpper() != "RET" ? -offset: offset;
                }
                BLEU(n, previous, toRet, offset);
                toRet = 0;
                previous = current + 1;
            }
        }

        Console.WriteLine("Raw " + instances/size);
        Console.WriteLine("Average score " + gAccumulator/(float)gCount);
    }

    private static Int32 gCount = 0;
    private static float gAccumulator = 0.0f;

    private static void BLEU(Int32 n, Int32 ndx, Int32 count, Int32 offset)
    {
        Int32 distance = Math.Abs(offset);
        if(distance > count || distance > 16)
        {
            offset = 0;
        }

        Instruction[] reference = new Instruction[count];
        Instruction[] candidate = new Instruction[count + offset];
        Array.Copy(gPortedBinary.run, ndx, reference, 0, count);
        Array.Copy(gSynthetic.run, ndx, candidate, 0, count + offset);

        String sentence = String.Empty;
        for(int i = 0; i < count; i++)
        {
            sentence += reference[i].m + " ";
        }
        sentence = sentence.Trim();

        Int32 found = 0;
        Int32 cursor = 0;
        HashSet<String> ngrams = new HashSet<String>();
        while(cursor < (count + offset)-n)
        {
            String ngram = String.Empty;
            for(int i = 0; i < n; i++)
            {
                ngram += candidate[cursor + i].m + " ";
            }
            ngram = ngram.Trim();
            if(ngrams.Add(ngram))
            {
                Int32 next = sentence.IndexOf(ngram);
                while(sentence != String.Empty && next != -1)
                {
                    found++;
                    Int32 start = next + ngram.Length;
                    if(start < sentence.Length)
                    {
                        sentence = sentence.Substring(start);
                        next = sentence.IndexOf(ngram);
                    }
                    else
                    {
                        next = -1;
                    }
                }
            }
            cursor++;
        }

        float score = 0.0f;
        if(ngrams.Count > 0)
        {
            score = found/(float)ngrams.Count;
        }

        gAccumulator+=score;
        gCount++;
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