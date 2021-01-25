using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Correlate
{
    public static void Main(String[] Args)
    {
        Int32 grams = Int32.Parse(Args[2])-1;
        var found = ngrams(Args[0], Args[1], grams);

        Int32 highest = 0;
        String key = String.Empty;
        var output = new Dictionary<String, ArrayList>();
        while(found.Count > 0)
        {
            foreach(KeyValuePair<String,Int32> entry in found)
            {
                if(entry.Value > highest)
                {
                    highest = entry.Value;
                    key = entry.Key;
                }
            }
            found.Remove(key);

            String[] ngrams = key.Split("|");
            if(!output.ContainsKey(ngrams[0]))
            {
                output.Add(ngrams[0], new ArrayList());
            }
            output[ngrams[0]].Add(new Tuple<String, Int32>(ngrams[1], highest));
            highest = 0;
        }

        // TODO Update values from strict counts to percentages

        Console.WriteLine("{");
        foreach(KeyValuePair<String,ArrayList> entry in output)
        {
            Console.WriteLine("  \"" + entry.Key + "\":{");
            Int32 count = entry.Value.Count;
            for(Int32 i = 0; i < count-1; i++)
            {
                Tuple<String, Int32> sub = entry.Value[i] as Tuple<String, Int32>;
                Console.WriteLine("    \"" + sub.Item1 + "\":" + sub.Item2 + ",");
            }
            Tuple<String, Int32> final = entry.Value[count-1] as Tuple<String, Int32>;
            Console.WriteLine("    \"" + final.Item1 + "\":" + final.Item2);
            Console.WriteLine("  },"); // TODO Last entry should not have a comma
        }
        Console.WriteLine("}");
    }

    private static Dictionary<String, Int32> ngrams(String file1, String file2, Int32 grams)
    {
        var found = new Dictionary<String, Int32>();
        String[] lines1 = File.ReadAllLines(file1);
        String[] lines2 = File.ReadAllLines(file2);

        Int32 current = 0;
        foreach(String line in lines1)
        {
            Int32 ndx = 0;
            String[] opcodes1 = lines1[current].Split(" ");
            String[] opcodes2 = lines2[current].Split(" ");
            while(ndx < opcodes1.Length-grams && ndx < opcodes2.Length-grams)
            {
                String key1 = opcodes1[ndx];
                String key2 = opcodes2[ndx];
                for(Int32 i = 1; i <= grams; i++)
                {
                    key1 += "-" + opcodes1[ndx+i];
                    key2 += "-" + opcodes2[ndx+i];
                }

                String key = key1 + "|" + key2;
                if(found.ContainsKey(key))
                {
                    found[key] = found[key] +1;
                }
                else
                {
                    found.Add(key, 1);
                }
                ndx+=grams;
            }
            current++;
        }

        return found;
    }
}
