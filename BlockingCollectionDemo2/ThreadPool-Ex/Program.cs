using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThreadPool_Ex.Models;

namespace ThreadPool_Ex
{
    class Programs
    {
        static void Main()
        {
            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            RunDTOTest.PassingAnOpenConnection();
            stopwatch1.Stop();
            Console.WriteLine("Comsuming time without BlockingCollection: {0}", stopwatch1.Elapsed);

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //AddTakeDemo.BC_AddTakeCompleteAdding();
            //stopwatch.Stop();
            //Console.WriteLine("Comsuming time with BlockingCollection non bound: {0}", stopwatch.Elapsed);

            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            FromToAnyDemo.BC_FromToAny().GetAwaiter().GetResult();
            Console.WriteLine("Done");
            stopwatch2.Stop();
            Console.WriteLine("Comsuming time with BlockingCollection with 10 worker: {0}", stopwatch2.Elapsed);

            //TryTakeDemo.BC_TryTake();
            //FromToAnyDemo.BC_FromToAny();
            //ConsumingEnumerableDemo.BC_GetConsumingEnumerable().GetAwaiter().GetResult();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    class RunDTOTest
    {
        public static void PassingAnOpenConnection()
        {
            using (var connection = new ProductContext())
            {
                for (int i = 0; i < 100; i++)
                {
                    var product = new Product { Id = i, Name = String.Format("Nam{0}", i) };
                    connection.Products.Add(product);
                    connection.SaveChanges();
                }
            }
        }
    }

    class AddTakeDemo
    {
        // Demonstrates:
        //      BlockingCollection<T>.Add()
        //      BlockingCollection<T>.Take()
        //      BlockingCollection<T>.CompleteAdding()
        public static async Task BC_AddTakeCompleteAdding()
        {
            using (BlockingCollection<int> bc = new BlockingCollection<int>())
            {
                // Spin up a Task to populate the BlockingCollection
                using (var connection = new ProductContext())
                {
                    using (Task t1 = Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            var product = new Product { Id = i, Name = String.Format("Nam{0}", i) };
                            connection.Products.Add(product);
                            bc.Add(connection.SaveChanges());
                        }
                        bc.CompleteAdding();
                    }
                    ))
                    {
                        // Spin up a Task to consume the BlockingCollection
                        using (Task t2 = Task.Run(() =>
                        {
                            try
                            {
                                // Consume consume the BlockingCollection
                                while (true) bc.Take();
                            }
                            catch (InvalidOperationException)
                            {
                                // An InvalidOperationException means that Take() was called on a completed collection
                                Console.WriteLine("That's All!");
                            }
                        }))
                        {
                            await Task.WhenAll(t1, t2);
                        }
                    }
                }
            }
        }
        public static async Task BC_AddTakeCompleteAddingTest()
        {
            using (BlockingCollection<int> bc = new BlockingCollection<int>())
            {
                // Spin up a Task to populate the BlockingCollection
                using (var connection = new ProductContext())
                {
                    for (int i = 0; i < 100; i++)
                    {
                        using (Task t1 = Task.Run(() =>
                        {
                            var product = new Product { Id = i, Name = String.Format("Nam{0}", i) };
                            connection.Products.Add(product);
                            bc.Add(connection.SaveChanges());
                            bc.CompleteAdding();
                        }))
                        {
                            // Spin up a Task to consume the BlockingCollection
                            using (Task t2 = Task.Run(() =>
                            {
                                try
                                {
                                    // Consume consume the BlockingCollection
                                    while (true) bc.Take();
                                }
                                catch (InvalidOperationException)
                                {
                                    // An InvalidOperationException means that Take() was called on a completed collection
                                    Console.WriteLine("That's All!");
                                }
                            }))
                            {
                                await Task.WhenAll(t1, t2);
                            }
                        }
                    }
                }
            }
        }
    }

    class TryTakeDemo
    {
        // Demonstrates:
        //      BlockingCollection<T>.Add()
        //      BlockingCollection<T>.CompleteAdding()
        //      BlockingCollection<T>.TryTake()
        //      BlockingCollection<T>.IsCompleted
        public static void BC_TryTake()
        {
            // Construct and fill our BlockingCollection
            using (BlockingCollection<int> bc = new BlockingCollection<int>())
            {
                int NUMITEMS = 10000;
                for (int i = 0; i < NUMITEMS; i++) bc.Add(i);
                bc.CompleteAdding();
                int outerSum = 0;

                // Delegate for consuming the BlockingCollection and adding up all items
                Action action = () =>
                {
                    int localItem;
                    int localSum = 0;

                    while (bc.TryTake(out localItem)) localSum += localItem;
                    Interlocked.Add(ref outerSum, localSum);
                };

                // Launch three parallel actions to consume the BlockingCollection
                Parallel.Invoke(action, action, action);

                Console.WriteLine("Sum[0..{0}) = {1}, should be {2}", NUMITEMS, outerSum, ((NUMITEMS * (NUMITEMS - 1)) / 2));
                Console.WriteLine("bc.IsCompleted = {0} (should be true)", bc.IsCompleted);
            }
        }
    }

    class FromToAnyDemo
    {
        // Demonstrates:
        //      Bounded BlockingCollection<T>
        //      BlockingCollection<T>.TryAddToAny()
        //      BlockingCollection<T>.TryTakeFromAny()
        public static async Task BC_FromToAny()
        {
            BlockingCollection<int>[] bcs = new BlockingCollection<int>[2];
            bcs[0] = new BlockingCollection<int>(5); // collection bounded to 5 items
            bcs[1] = new BlockingCollection<int>(5); // collection bounded to 5 items

            using (var connection = new ProductContext())
            {
                int numFailures = 0;

                for (int i = 0; i < 100; i++)
                {
                    using (Task t1 = Task.Run(() =>
                    {
                        var product = new Product { Id = i, Name = String.Format("Nam{0}", i) };
                        connection.Products.Add(product);

                        // Should be able to add 10 items w/o blocking
                        if (BlockingCollection<int>.TryAddToAny(bcs, connection.SaveChanges()) == -1) numFailures++;
                    }))
                    {
                        using (Task t2 = Task.Run(() =>
                            {
                                Console.WriteLine("TryAddToAny: {0} failures (should be 0)", numFailures);
                                    // Should be able to retrieve 10 items
                                    int numItems = 0;
                                int item;
                                while (BlockingCollection<int>.TryTakeFromAny(bcs, out item) != -1) numItems++;
                                Console.WriteLine("TryTakeFromAny: retrieved {0} items (should be 10)", numItems);
                            })
                        )
                        {
                            await Task.WhenAll(t1, t2);
                        }
                    }
                }
                //}
            }
        }
    }
}

class ConsumingEnumerableDemo
{
    // Demonstrates:
    //      BlockingCollection<T>.Add()
    //      BlockingCollection<T>.CompleteAdding()
    //      BlockingCollection<T>.GetConsumingEnumerable()
    public static async Task BC_GetConsumingEnumerable()
    {
        using (BlockingCollection<int> bc = new BlockingCollection<int>())
        {
            // Kick off a producer task
            await Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    bc.Add(i);
                    await Task.Delay(100); // sleep 100 ms between adds
                }

                // Need to do this to keep foreach below from hanging
                bc.CompleteAdding();
            });

            // Now consume the blocking collection with foreach.
            // Use bc.GetConsumingEnumerable() instead of just bc because the
            // former will block waiting for completion and the latter will
            // simply take a snapshot of the current state of the underlying collection.
            foreach (var item in bc.GetConsumingEnumerable())
            {
                Console.WriteLine(item);
            }
        }
    }
}
