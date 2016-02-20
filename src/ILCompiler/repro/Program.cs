using System;
using System.Threading.Tasks;

class Example
{
    static object locker = new object();

    static void Delay(int count)
    {
        for (int i = 0; i < count * 1000; ++i)
        {
            Console.Write("");
        }
    }

    static void Main(string[] args)
    {
        String taskData = "delta";
        Task[] tasks = new Task[3];
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i] = Task.Factory.StartNew((pos) =>
            {
                int ipos = (int)pos;
                Console.WriteLine(ipos);
                Delay((int)Math.Pow(10, (2 - ipos)) * 100);
                Console.WriteLine("Trying to acquire lock {0}", ipos);
                lock (locker)
                {
                    Console.WriteLine("Acquired lock {0}", ipos);
                    if (ipos == 1) Delay(100000);
                    Console.WriteLine("Task={0}, obj={1}, i={2}", Task.CurrentId, taskData, ipos);
                    Console.WriteLine("Released lock {0}", ipos);
                }
            }, i);
        }
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i].Wait();
        }
    }
}


