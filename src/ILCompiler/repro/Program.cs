using System;
using System.Diagnostics;
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
        //String taskData = "delta";
        Task[] tasks = new Task[3];
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i] = Task.Factory.StartNew((pos) =>
            {
                lock (locker)
                {
                    Console.WriteLine(pos);
                }
            }, i);
        }
        for (int i = 0; i < tasks.Length; ++i)
        {
            tasks[i].Wait();
        }
    }
}


