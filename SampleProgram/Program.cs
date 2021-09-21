using System;

namespace SampleProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            int time = _Debug_Weaver();
            string timeStr = $"{time / 3600:00}:{(time / 60) % 60:00}:{time % 60:00}";
            Console.WriteLine($"Core Time is {timeStr}");
        }
        public static int _Debug_Weaver()
        {
            // weaver will change this to return (min*60+sec)
            return 0;
        }
    }
}
