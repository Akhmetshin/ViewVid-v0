using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ConsoleTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            // возьмём любой текстовый файл
            // нам нужно вывести на экран строку(ки) с максимальным количеством слов.
            using (StreamReader reader = new StreamReader(File.Open(@"C:\Temp\.gitignore", FileMode.Open)))
            {
                int max = -1;
                string line;

                // это код с примера для StreamReader Конструкторы
                while (reader.Peek() >= 0) // цикл нормально отрабатывает
                {
                    line = reader.ReadLine();
                    string[] subs = line.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (subs.Length > max) max = subs.Length; // найдём максимальное количество слов в строке
                }
                
                Console.WriteLine("max={0}", max);

                reader.BaseStream.Position = 0; // позиция reader нормально встаёт в 0

                Console.WriteLine("while (reader.Peek() >= 0)");

                while (reader.Peek() >= 0) // но, reader.Peek() возвращает -1. непонятно.
                { // в цикл не заходим
                    line = reader.ReadLine();
                    string[] subs = line.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (subs.Length == max)
                    {
                        Console.WriteLine(line);
                    }
                } // наверное, нужно ещё что-то, чтобы reader вздрогнул и Peek() заработал нормально

                Console.WriteLine("while (null != (str = reader.ReadLine()))");

                while (null != (line = reader.ReadLine())) // этот цикл тоже нормально отрабатывает
                {
                    string[] subs = line.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (subs.Length == max)
                    {
                        Console.WriteLine(line);
                    }
                }
            }
        }
    }
}
