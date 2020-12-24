using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zadatak1.Demo
{
    class Program
    {
        static void Main()
        {
            // number of threads
            // max duration
            // preemption allowed
            // use system scheduler

            List<string> messages = new List<string>();

            CustomTaskScheduler t = new CustomTaskScheduler(2, 3800, true, false);
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.Low, 5000, new List<Resource>(new Resource[] { CustomTaskScheduler.Resources[1] }));
            Task.Delay(2000).Wait();
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.High, 1600, new List<Resource>(new Resource[] { CustomTaskScheduler.Resources[2] })); 
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.Medium, 1200, new List<Resource>(new Resource[] { CustomTaskScheduler.Resources[2] })); 
            /*t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.Micro, 2700); 
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.Urgent); 
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.Medium); 
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.High, 3500);
            t.Schedule(x => { lock (messages) messages.Add(x); }, priority: CustomTaskScheduler.Priority.High, 3500, new List<Resource>(new Resource[] { CustomTaskScheduler.Resources[2], CustomTaskScheduler.Resources[4] }));
          */
            
            char more;
            do
            {
                Console.WriteLine("Enter Task Details:");
                Console.Write("Priority (1-5) (Press enter to leave): ");
                int priority = 0;
                String priorityLine = Console.ReadLine();
                if (!priorityLine.Replace(" ", "").Equals(""))
                    priority = Int32.Parse(priorityLine);
                else
                    break;

                Console.Write("Duration (optional): ");
                String line = Console.ReadLine();
                int duration = 0;
                if (!line.Replace(" ", "").Equals(""))
                    duration = Int32.Parse(line);

                Console.WriteLine("Do you want do asign resources for process (Y/N)? (optional)");
                char q = Char.Parse(Console.ReadLine());
                List<Resource> resources = null;
                if (q == 'Y' || q == 'y')
                {
                    resources = new List<Resource>();
                    char res = 'n';
                    do
                    {
                        Console.WriteLine("Resources (just number from 0 to 11 including 11)");
                        try
                        {
                            int index = Int32.Parse(Console.ReadLine());
                            if (index >= 0 && index <= 6 && !resources.Contains(CustomTaskScheduler.Resources[index]))
                                resources.Add(CustomTaskScheduler.Resources[index]);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("Wrong input");
                        }

                        Console.WriteLine("More resources (Y/N)? ");
                        res = Char.Parse(Console.ReadLine());

                    } while (res == 'y' || res == 'Y');
                }

                if (duration != 0)
                    if (resources == null)
                        t.Schedule(x => { lock (messages) messages.Add(x); }, priority, duration);
                    else
                        t.Schedule(x => { lock (messages) messages.Add(x); }, priority, duration, resources);
                else
                    if (resources == null)
                        t.Schedule(x => { lock (messages) messages.Add(x); }, priority);
                    else
                        t.Schedule(x => { lock (messages) messages.Add(x); }, priority, resources);

                Console.Write("Add More Tasks (Y/N)? ");
                more = Char.Parse(Console.ReadLine());

            } while (more == 'Y' || more == 'y'); 

            Console.WriteLine("Executing...");

            while (t.NumberOfExecutingTasks > 0)
                Task.Delay(100).Wait();
            
            Console.WriteLine("Tasks that have been executed: ");
            foreach (string message in messages)
                Console.WriteLine(message);

            Console.WriteLine("Done.");

            Console.ReadLine();
        }
    }
}
