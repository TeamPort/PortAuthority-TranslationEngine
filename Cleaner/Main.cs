using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Clearer
{
    public static void Main(String[] Args)
    {
        Int32 count = 0;
        ArrayList modified = new ArrayList();
        String[] lines = File.ReadAllLines(Args[0]);
        foreach(String line in lines)
        {
            String temp = line;
            if(count < lines.Length-2)
            {
                if(lines[count+1].Contains('}'))
                {
                    temp = line.Replace(",", String.Empty);
                }
            }
            modified.Add(temp);
            count++;
        }
        File.WriteAllLines(Args[0], (String[])modified.ToArray(typeof(String)));
    }
}
