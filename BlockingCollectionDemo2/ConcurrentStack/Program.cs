﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentStack
{
    class Program
    {
        static void Main(string[] args)
        {
            int items = 10000;

            ConcurrentStack<int> stack = new ConcurrentStack<int>();

            // Create an action to push items onto the stack
            Action pusher = () =>
            {
                for (int i = 0; i < items; i++)
                {
                    stack.Push(i);
                }
            };

            // Run the action once
            pusher();
            int result;
            if (stack.TryPeek(out result))
            {
                Console.WriteLine($"TryPeek() saw {result} on top of the stack.");
            }
            else
            {
                Console.WriteLine("Could not peek most recently added number.");
            }

            // Empty the stack
            stack.Clear();

            if (stack.IsEmpty)
            {
                Console.WriteLine("Cleared the stack.");
            }

            // Create an action to push and pop items
            Action pushAndPop = () =>
            {
                Console.WriteLine($"Task started on {Task.CurrentId}");

                int item;
                for (int i = 0; i < items; i++)
                    stack.Push(i);
                for (int i = 0; i < items; i++)
                    stack.TryPop(out item);

                Console.WriteLine($"Task ended on {Task.CurrentId}");
            };

            // Spin up five concurrent tasks of the action
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Factory.StartNew(pushAndPop);

            // Wait for all the tasks to finish up
            Task.WhenAll(tasks).GetAwaiter().GetResult();
            if (!stack.IsEmpty)
            {
                Console.WriteLine("Did not take all the items off the stack");
            }
        }
    }
}
