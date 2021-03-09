using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

public class Synthetic
{
    private static bool initialized = false;
    private const float densityRatio = 1.057155f;
    private static Dictionary<String, Int32> usedMap = new Dictionary<String, Int32>();
    private static Dictionary<String, Object> ngrams = null;

    private static void setup(String ngram)
    {
        if(initialized) return;

        String data = String.Empty;
        var n = ngram.Split("-").Length;
        switch(n)
        {
            case 2:
                data = @"data/ngrams/bigrams.js";
                break;
            case 3:
                data = @"data/ngrams/trigrams.js";
                break;
        }
        if(data == String.Empty) return;

        var Deserializer = new JavaScriptSerializer();
        ngrams = Deserializer.Deserialize<Dictionary<String, Object>>(File.ReadAllText(data));
        initialized = true;
    }

    private static String emit(String ngram)
    {
        if(!ngrams.ContainsKey(ngram)) return String.Empty;

        Dictionary<String, Object> test = (Dictionary<String, Object>)ngrams[ngram];
        String[] keys = new String[test.Count];
        test.Keys.CopyTo(keys, 0);
        var key = keys[0];
        if(usedMap.ContainsKey(ngram))
        {
            var ndx = 0;
            var value = (int)test[key];
            var current = usedMap[ngram];
            while(current >= value && current < 100)
            {
                ++ndx;
                if(ndx < keys.Length)
                {
                    value += (int)test[keys[ndx]];
                }
                else
                {
                    ndx = 0;
                    current = 0;
                    break;
                }
            }
            key = keys[ndx];
            usedMap[ngram] = current+1;
        }
        else
        {
            usedMap[ngram] = 1;
        }

        return key;
    }

    static public Data buildSyntheticInstanceMap(Data data, Int32 n)
    {
        if(!data.triple.Contains("x86_64")) return null;

        Data generatedData = new Data();
        generatedData.triple = "aarch64-unknown-linux-gnu";

        List<Instruction> list = new List<Instruction>();

        var processed = 0;
        String[] parts = {String.Empty, String.Empty, String.Empty, String.Empty};
        Dictionary<String, Int32> synthMap = new Dictionary<String, Int32>();
        for(var i = 0; i < data.run.Length-(n-1); i+=n)
        {
            for(var k = 0; k < n; k++)
            {
                parts[k] = data.run[i + k].m.ToUpper();
            }

            var ngram = String.Empty;
            for(var k = 0; k < n-1; k++)
            {
                ngram += parts[k] + "-";
            }
            ngram += parts[n-1];

            var rand = new Random();
            setup(ngram);
            var generated = emit(ngram);
            if(generated == String.Empty)
            {
                // Aid transistion points not served by the static ngram generation tool
                if(ngram[0] == 'J')
                {
                    String guess = rand.Next(0, 1) == 0 ? "MOV": "ADD";
                    generated = "B.NE-" + guess;
                    for(var l = 0; l < n-2; l++)
                    {
                        generated += "-INVALID";
                    }
                }
                else if(ngram.Split('-')[0] == "RET")
                {
                    generated = "RET-ADD";
                    for(var l = 0; l < n-2; l++)
                    {
                        generated += "-INVALID";
                    }
                }
                else if(ngram.Contains("JZ"))
                {
                    generated = emit(ngram.Replace("JZ", "JE"));
                }
                else if(ngram.Contains("JNZ"))
                {
                    generated = emit(ngram.Replace("JNZ", "JNE"));
                }

                if(generated == "")
                {
                    for(var k = 0; k < n-1; k++)
                    {
                        generated += "INVALID-";
                    }
                    generated += "INVALID";
                }
            }

            var instructions = generated.Split("-");
            for(var j = 0; j < instructions.Length; j++)
            {
                var value = 0;
                var instruction = instructions[j];
                if(synthMap.ContainsKey(instruction))
                {
                    value = synthMap[instruction];
                }

                value++;
                processed++;
                synthMap[instruction] = value;

                Instruction replay = new Instruction();
                replay.m = instruction;
                list.Add(replay);
            }
        }

        var total = Math.Floor(data.run.Length*densityRatio);
        var needed = total - processed;

        var Deserializer = new JavaScriptSerializer();
        Dictionary<String, Object> aarch64_distribution = Deserializer.Deserialize<Dictionary<String, Object>>(File.ReadAllText(@"data/distribution/aarch64.js"));

        var m = 0;
        String[] keys = new String[aarch64_distribution.Count];
        aarch64_distribution.Keys.CopyTo(keys, 0);
        while(needed > 0)
        {
            if(m >= aarch64_distribution.Count)
            {
                m = 0;
            }

            var value = 0;
            var instruction = keys[m];
            if(synthMap.ContainsKey(instruction))
            {
                value = synthMap[instruction];
            }

            value+=(int)aarch64_distribution[instruction];
            synthMap[instruction] = value;
            needed-=(int)aarch64_distribution[instruction];
            m++;

            Instruction replay = new Instruction();
            replay.m = instruction;
            list.Add(replay);
        }

        generatedData.run = list.ToArray();

        Dictionary<String, String> aarch64_dummy = Deserializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(@"data/dummy/aarch64.js"));
        using(BinaryWriter writer = new BinaryWriter(File.Open("a.out", FileMode.Create)))
        {
            foreach(var instruction in list)
            {
                if(aarch64_dummy.ContainsKey(instruction.m))
                {
                    Int32 value = Convert.ToInt32(aarch64_dummy[instruction.m] , 16);
                    writer.Write(value);
                }
                else if(instruction.m.Contains("B."))
                {
                    Int32 value = Convert.ToInt32(aarch64_dummy["B.NE"], 16); // B.cond
                    writer.Write(value);
                }
                else if(instruction.m.Contains("INVALID"))
                {
                    // Do nothing
                }
                else
                {
                    Console.WriteLine("Missing " + instruction.m);
                }
            }
        }

        return generatedData;
    }
}
