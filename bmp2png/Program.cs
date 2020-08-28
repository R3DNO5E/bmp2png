using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace bmp2png
{
    class Program
    {
        static Queue<string> tasks = new Queue<string>();
        static bool flag_rm = false;

        class ConvertTaskExecutor
        {
            static void conv_file(string path)
            {
                Console.WriteLine(path);

                var img = Image.FromFile(path);
                img.Save(path.Substring(0, path.Length - 4) + ".png", ImageFormat.Png);
                img.Dispose();

                if (flag_rm)
                {
                    File.Delete(path);
                }
            }

            static int executeTask()
            {
                string path = "";
                lock (tasks)
                {
                    try
                    {
                        path = tasks.Dequeue();
                    }
                    catch(InvalidOperationException e)
                    {
                        return -1;
                    }
                }
                conv_file(path);
                return 0;
            }

            public static void run()
            {
                while (executeTask() == 0) ;
            }
        }

        static void add_file(string path)
        {
            if (path.Substring(path.Length - 4).CompareTo(".bmp") == 0)
            {
                tasks.Enqueue(path);
            }
        }

        static void list_dir(string path)
        {
            foreach(var e in Directory.GetFiles(path))
            {
                add_file(e);
            }
            foreach(var e in Directory.GetDirectories(path))
            {
                list_dir(e);
            }
        }

        static void Main(string[] args)
        {
            foreach (var e in args)
            {
                if (e.Substring(0, 2).CompareTo("--") == 0)
                {
                    switch (e)
                    {
                        case "--rm":
                            flag_rm = true;
                            break;
                        default:
                            Console.WriteLine("Unknown Option: " + e);
                            return;
                    }
                } else if(File.Exists(e) || Directory.Exists(e)) {
                    if (File.GetAttributes(e).HasFlag(FileAttributes.Directory))
                    {
                        list_dir(e);
                    }
                    else
                    {
                        add_file(e);
                    }
                }
            }

            int threads = Environment.ProcessorCount;

            Console.WriteLine("Converting " + tasks.Count.ToString() + " files on " + threads.ToString() + " worker threads...");

            var task_r = new Task[threads];
            for(int i = 0;i < threads;i++)
            {
                task_r[i] = Task.Run(() => ConvertTaskExecutor.run());
            }

            Task.WaitAll(task_r);
        }
    }
}
