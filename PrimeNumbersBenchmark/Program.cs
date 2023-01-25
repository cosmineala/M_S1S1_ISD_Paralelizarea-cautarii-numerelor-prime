using Cloo.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace PrimeNumbersBenchmark
{
    class Program
    {
        //static int MAX = 20_000_000;
        //static int MAX = 100_000;
        static int MAX = 10_000_000;
        static void Main(string[] args)
        {

            TestCPU();

            TestGPU(args);

            Console.Write("ALL DONE !!!");
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
        }
        static void TestCPU()
        {
            string CPUs = "";
            ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (ManagementObject mo in mos.Get())
            {
                CPUs += mo["Name"];
            }

            Console.WriteLine("Prime numbers benchmark V 1.2 (Computing primes number up to 20,000,000 and save them as array)\n ");
            Console.WriteLine("\tTesting CPU, Model: " + CPUs + ", " + Environment.ProcessorCount + " Threads available: \n");

            SingleCPU();
            Console.WriteLine();
            MultiCPU();
        }

        static void SingleCPU()
        {
            Console.WriteLine("\t\tRunig Single core test...");
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            //LinkedList<int> list = new LinkedList<int>();
            int[] arr = new int[MAX];
            for( int i=0; i<MAX; i++)
            {
                if( IsPrime(i))
                {
                    //list.AddLast(i);
                    arr[i] = i;
                }
                else
                {
                    arr[i] = 0;
                }
            }
            arr = arr.Where(i => i != 0).ToArray();
            stopwatch.Stop();

            Console.WriteLine("\t\tDone in " + (float)stopwatch.ElapsedMilliseconds / 1000 + " seconds");
        }
        static void MultiCPU()
        {
            Console.WriteLine("\t\tRunig Multi core test...");
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            //object lockObj = new object();
            //LinkedList<int> list = new LinkedList<int>();
            int[] arr = new int[MAX];
            Parallel.For(0, MAX, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                if (IsPrime(i))
                {
                    //list.AddLast(i);
                    arr[i] = i;
                }
                else
                {
                    arr[i] = 0;
                }
            });
            arr = arr.Where(i => i != 0).ToArray();
            stopwatch.Stop();

            Console.WriteLine("\t\tDone in " + (float)stopwatch.ElapsedMilliseconds / 1000 + " seconds");
        }
        static bool IsPrime( int num)
        {
            int upper = (int)Math.Sqrt(num);
            for (int i = 2; i < upper; i++)
            {
                if (i % num == 0)
                {
                    return false;
                }
            }
            return true;
        }

        static void TestGPU(string[] args)
        {
            Console.WriteLine("\n\tTesting GPU:\n");
            var devices = ClooExtensions.GetDeviceNames();

            foreach (var dev in devices.Where(d => args.Length == 0 || args.Contains(d.Trim())))
            {
                //Console.WriteLine(dev.Trim());
                Console.WriteLine("\t\t" + dev);

                int[] primes = Enumerable.Range(0, MAX).ToArray();

                var sw = new Stopwatch();
                try
                {
                    sw.Start();
                    primes.ClooForEach(IsPrimeCL, k => true, (i, d, v) => d == dev);
                    primes = primes.Where(n => n != 0).ToArray();
                    sw.Stop();

                    //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\PrimNumMMM.txt";
                    //var outStr = string.Join(" ", primes );
                    //System.IO.File.WriteAllText(@"C:\Users\cosmi\Documents\GitHub\C-Sharp\PrimeNumbersBenchmark\PrimeNumbersBenchmark\Outputs\Out.txt", outStr );
                    
                    // var path = @"C:\Users\cosmi\Documents\GitHub\C-Sharp\PrimeNumbersBenchmark\Out.txt";
                    // File.WriteAllText(path, string.Join(", ", primes.Take(10) ));
                    //Console.WriteLine($"{string.Join(", ", primes.Take(10))}, ...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {

                    Console.WriteLine($"\t\tTime: { (double)sw.ElapsedMilliseconds / 1000} seconds");
                }

                Console.WriteLine();
            }
        }
        static string IsPrimeCL
        {
            get
            {
                return
                    @"
                    kernel void GetIfPrime(global int* message)
                    {
                        int index = get_global_id(0);
                        int upperl=(int)sqrt((float)message[index]);
                        for(int i=2;i<=upperl;i++)
                        {
                            if(message[index]%i==0)
                            {
                                message[index]=0;
                                return;
                            }
                        }
                    }";
            }
        }
    }
}
