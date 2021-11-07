using System;
using System.IO;

class Test
{
    public static void Main()
    {
        string path = @"c:\temp\MyTest.txt";

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.CreateNew)))
            {
                sw.WriteLine("This");
                sw.WriteLine("is some text");
                sw.WriteLine("to test");
                sw.WriteLine("reading");
            }

            using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                while (sr.Peek() >= 0) // цикл нормально отрабатывает
                {
                    Console.WriteLine(sr.ReadLine());
                }

                //----------------------------------------------------------------------------------------------------------------------
                // может понадобиться повторный проход по файлу
                sr.BaseStream.Position = 0; // позиция sr нормально встаёт в 0
                
                while (sr.Peek() >= 0) // но, sr.Peek() возвращает -1. непонятно.
                { // в цикл не заходим
                    Console.WriteLine(sr.ReadLine());
                } // наверное, нужно ещё что-то, чтобы sr вздрогнул и Peek() заработал нормально

                string line;
                while (null != (line = sr.ReadLine())) // этот цикл тоже нормально отрабатывает
                {
                    Console.WriteLine(line);
                }
                //----------------------------------------------------------------------------------------------------------------------
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The process failed: {0}", e.ToString());
        }
    }
}