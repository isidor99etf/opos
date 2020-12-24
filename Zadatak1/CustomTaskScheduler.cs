using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Zadatak1
{
    public class CustomTaskScheduler
    {
        private class ImprovedSystemTaskScheduler : TaskScheduler
        {

            [ThreadStatic]
            private static bool isItemProcessed;

            private readonly LinkedList<Task> tasks = new LinkedList<Task>(); 

            private readonly int numberOfThreads;

            private int delegatesQueuedOrRunning = 0;

            public ImprovedSystemTaskScheduler(int maxDegreeOfParallelism)
            {
                if (maxDegreeOfParallelism < 1) 
                    throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
                    
                numberOfThreads = maxDegreeOfParallelism;
            }

            protected sealed override void QueueTask(Task task)
            {
                lock (tasks)
                {
                    tasks.AddLast(task);
                    if (delegatesQueuedOrRunning < numberOfThreads)
                    {
                        ++delegatesQueuedOrRunning;
                        NotifyThreadPoolOfPendingWork();
                    }
                }
            }

            private void NotifyThreadPoolOfPendingWork()
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    isItemProcessed = true;
                    try
                    {
                        while (true)
                        {
                            Task item;
                            lock (tasks)
                            {
                                if (tasks.Count == 0)
                                {
                                    --delegatesQueuedOrRunning;
                                    break;
                                }

                                item = tasks.First.Value;
                                tasks.RemoveFirst();
                            }

                            base.TryExecuteTask(item);
                        }
                    }
                    finally 
                    {
                        isItemProcessed = false; 
                    }
                }, null);
            }

            protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                if (!isItemProcessed) return false;

                if (taskWasPreviouslyQueued)
                    if (TryDequeue(task))
                        return base.TryExecuteTask(task);
                    else
                        return false;
                else
                    return base.TryExecuteTask(task);
            }

            // because of priority
            public void Dequeue(Task task) => TryDequeue(task);

            // Attempt to remove a previously scheduled task from the scheduler.
            protected sealed override bool TryDequeue(Task task)
            {
                lock (tasks)
                {
                    return tasks.Remove(task);
                }
            }

            protected sealed override IEnumerable<Task> GetScheduledTasks()
            {
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(tasks, ref lockTaken);
                    if (lockTaken) return tasks;
                    else throw new NotSupportedException();
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(tasks);
                }
            }
        }

        // simulate resources
        // there are 7 available resources
        public static readonly Resource[] Resources =
        {
            new Resource("R1"),
            new Resource("R2"),
            new Resource("R3"),
            new Resource("R4"),
            new Resource("R5"),
            new Resource("R6"),
            new Resource("R7"),
            new Resource("R8"),
            new Resource("R9"),
            new Resource("R10"),
            new Resource("R11"),
        };

        private static object ScheduleLockObject = new object();

        private static System.IO.StreamWriter writer;

        public static class Priority
        {
            public const int Urgent = 1;
            public const int High = 2;
            public const int Medium = 3;
            public const int Low = 4;
            public const int Micro = 5;

            public static String GetPriorityString(int priority)
            {
                switch (priority)
                {
                    case Urgent:
                        return UrgentString;
                    case High:
                        return HighString;
                    case Medium:
                        return MediumString;
                    case Low:
                        return LowString;
                    case Micro:
                        return MicroString;
                    default:
                        return null;
                }
            }

            private const String UrgentString = "URGENT";
            private const String HighString = "HIGH";
            private const String MediumString = "MEDIUM";
            private const String LowString = "LOW";
            private const String MicroString = "MICRO";

        }

        public int NumberOfThreads { get; private set; }
        public int MaxDuration { get; private set; }

        public bool IsPreemptive { get; private set; }

        public bool UseSystemTaskScheduler { get; private set; }

        public int NumberOfExecutingTasks => executableTasks.Count(x => x.HasValue);

        private readonly List<UserTask> pendingTasks = new List<UserTask>();

        private readonly (Task task, UserTask userTask)?[] executableTasks;

        private readonly ImprovedSystemTaskScheduler improvedSystemTaskScheduler;

        private TaskFactory taskFactory;

        private CancellationTokenSource tokenSource;

        public CustomTaskScheduler(int numberOfThreads, int maxDuration, bool isPreemptive, bool useSystemTaskScheduler)
        {
            NumberOfThreads = numberOfThreads;
            MaxDuration = maxDuration;
            IsPreemptive = isPreemptive;
            UseSystemTaskScheduler = useSystemTaskScheduler;

            executableTasks = new (Task, UserTask)?[numberOfThreads];

            if (useSystemTaskScheduler)
            {
                improvedSystemTaskScheduler = new ImprovedSystemTaskScheduler(numberOfThreads);
                taskFactory = new TaskFactory(improvedSystemTaskScheduler);
                tokenSource = new CancellationTokenSource();
            }

            string logFilePath = System.IO.Directory.GetCurrentDirectory() + @".\log.txt";
            Console.WriteLine(logFilePath);
            writer = System.IO.File.CreateText(logFilePath);
        }

        ~CustomTaskScheduler()
        {
            writer.Flush();
            writer.Close();
        }

        /*public void PrintPendingTasks()
        {
            foreach (UserTask task in pendingTasks)
            {
                Console.WriteLine($"Task ID: {task.TaskId}, Task Priority: {Priority.GetPriorityString(task.Priority)}, Task Duration: {task.Duration}, Task Executed On Thread: {task.CurrentThreadId}, Task Resources Count: {task.Resources.Count}, Resources: {String.Join("--", task.Resources)}");     
            }
        }*/

        public void Schedule(UserTask.Job job, int priority)
        {
            Schedule(job, priority, MaxDuration);
        }

        public void Schedule(UserTask.Job job, int priority, int duration)
        {
            List<Resource> resources = GetRandomResources();
            Schedule(job, priority, duration, resources);
        }

        public void Schedule(UserTask.Job job, int priority, List<Resource> resources)
        {
            Schedule(job, priority, MaxDuration, resources);
        }

        public void Schedule(UserTask.Job job, int priority, int duration, List<Resource> resources)
        {
            pendingTasks.Add(new UserTask(job, priority, duration, resources));

            if (IsPreemptive)
                lock (ScheduleLockObject)
                {
                    ScheduleTaskOnThread();
                }
            else
                if (executableTasks.Any(x => !x.HasValue))
                    lock (ScheduleLockObject)
                    {
                        ScheduleTaskOnThread();
                    }
        }

        private void ScheduleTaskOnThread()
        {
            if (!IsPreemptive)
            {
                // there is no preemption
                int[] freeThreads = executableTasks.Select((value, index) => (value, index)).Where(x => !x.value.HasValue).Select(x => x.index).OrderBy(x => x).ToArray();
                foreach (int freeThread in freeThreads)
                {
                    // needs to check resources
                    // if the process does not have all the resources it will not even start running
                    UserTask taskToExecute = 
                        pendingTasks.OrderBy(x => x.Priority)
                                    .ThenBy(x => x.Duration)
                                    .FirstOrDefault(x => x.CurrentThreadId == -1 && x.Resources.All(r => !r.IsLocked));

                    if (taskToExecute != null)
                    {
                        taskToExecute.CurrentThreadId = freeThread;
                        taskToExecute.Resources.ForEach(r => 
                        { 
                            r.Lock(); 
                            r.SetTaskId(taskToExecute.TaskId);
                        });

                        Task task;
                        if (!UseSystemTaskScheduler)
                        {
                            task = CreateTask(taskToExecute);
                        } 
                        else
                        {
                            task = CreateTaskUsingFactory(taskToExecute);
                        }

                        executableTasks[freeThread] = (task, taskToExecute);
                    }
                }
            }
            else
            {
                // first check free threads
                int[] freeThreads = executableTasks.Select((value, index) => (value, index)).Where(x => !x.value.HasValue).Select(x => x.index).OrderBy(x => x).ToArray();
                if (freeThreads.Length > 0)
                {
                    foreach (int freeThread in freeThreads)
                    {
                        // needs to check resources
                        // if the process does not have all the resources it will not even start running
                        UserTask taskToExecute =
                            pendingTasks.OrderBy(x => x.Priority)
                                        .ThenBy(x => x.Duration)
                                        .FirstOrDefault(x => x.CurrentThreadId == -1);
                                                   
                        // check resources
                        if (taskToExecute != null)
                        {
                            // this simulates PIP
                            // small priority task inherits priority from higher priotiry task
                            if (taskToExecute.Resources.Any(r => r.IsLocked))
                            {
                                // if any resource is locked
                                List<Resource> resources = taskToExecute.Resources.Where(r => r.IsLocked).ToList();

                                foreach (Resource r in resources)
                                {
                                    UserTask userTask = pendingTasks.Where(x => x.TaskId == r.TaskId).FirstOrDefault();
                                    if (userTask != null && userTask.Priority > taskToExecute.Priority)
                                    {
                                        int priority = userTask.Priority;
                                        userTask.ChangePriority(taskToExecute.Priority);
                                        lock (writer)
                                        {
                                            writer.WriteLine("Task ID {0} : Priority Change From {1} to {2}", userTask.TaskId, Priority.GetPriorityString(priority), Priority.GetPriorityString(userTask.Priority));
                                            writer.Flush();
                                        }
                                    }
                                }
                            }

                            // task can be scheduled only if all resources are available
                            if (taskToExecute.Resources.All(r => !r.IsLocked))
                            {
                                taskToExecute.CurrentThreadId = freeThread;
                                taskToExecute.Resources.ForEach(r =>
                                {
                                    r.Lock();
                                    r.SetTaskId(taskToExecute.TaskId);
                                });

                                Task task;
                                if (!UseSystemTaskScheduler)
                                {
                                    task = CreateTask(taskToExecute);
                                }
                                else
                                {
                                    task = CreateTaskUsingFactory(taskToExecute);
                                }

                                executableTasks[freeThread] = (task, taskToExecute);
                                lock (writer)
                                {
                                    writer.WriteLine("Task Scheduled on Free Thread {0}, Task Priority: {1}", freeThread, taskToExecute.Priority);
                                    writer.Flush();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // sort execuding tasks by priority
                    int[] threads = executableTasks.Select((value, index) => (value.Value.userTask, index)).OrderByDescending(x => x.userTask.Priority).ThenByDescending(x => x.userTask.Duration).Select(x => x.index).ToArray();
                    foreach (int thread in threads)
                    {
                        UserTask taskToExecute =
                            pendingTasks.OrderBy(x => x.Priority)
                                        .ThenBy(x => x.Duration)
                                        .FirstOrDefault(x => x.CurrentThreadId == -1);

                        if (taskToExecute != null)
                        {
                            // check resources
                            if (taskToExecute.Resources.Any(r => r.IsLocked))
                            {
                                // if any resource is locked
                                List<Resource> resources = taskToExecute.Resources.Where(r => r.IsLocked).ToList();

                                foreach (Resource r in resources)
                                {
                                    UserTask userTask = pendingTasks.Where(x => x.TaskId == r.TaskId).FirstOrDefault();
                                    if (userTask != null && userTask.Priority > taskToExecute.Priority)
                                    {
                                        int priority = userTask.Priority;
                                        userTask.ChangePriority(taskToExecute.Priority);
                                        lock (writer)
                                        {
                                            writer.WriteLine("{0} Priority Change From {1} to {2}", userTask.TaskId, priority, userTask.Priority);
                                            writer.Flush();
                                        }
                                    }
                                }
                            }

                            // all resources must be available for task to start
                            if (taskToExecute.Resources.All(r => !r.IsLocked) && executableTasks[thread].HasValue)
                            { 
                                (Task taskOnThread, UserTask userTask) = executableTasks[thread].Value;
                                if (!taskToExecute.IsCompleted && taskToExecute.Priority < userTask.Priority)
                                {
                                    // there is task with greather priority
                                    // thread needs to switch tasks

                                    userTask.CurrentThreadId = -1;

                                    taskToExecute.CurrentThreadId = thread;
                                    taskToExecute.Resources.ForEach(r =>
                                    {
                                        r.Lock();
                                        r.SetTaskId(taskToExecute.TaskId);
                                    });

                                    Task task;
                                    if (!UseSystemTaskScheduler)
                                    {
                                        task = CreateTask(taskToExecute);
                                    }
                                    else
                                    {
                                        improvedSystemTaskScheduler.Dequeue(taskOnThread);
                                        task = CreateTaskUsingFactory(taskToExecute);
                                    }

                                    executableTasks[thread] = (task, taskToExecute);
                                    lock (writer)
                                    {
                                        writer.WriteLine("Task Scheduled, Preemption on Thread {0}, Task Priority: {1}", thread, taskToExecute.Priority);
                                        writer.Flush();
                                    }
                                }
                            }
                        }
                        else
                            break;
                    }
                }
            }
        }

        private void EndTask()
        {
            for (int i = 0; i < NumberOfThreads; ++i)
            {
                if (executableTasks[i].HasValue)
                {
                    (Task task, UserTask userTask) = executableTasks[i].Value;
                    if (userTask.IsCompleted)
                    {
                        pendingTasks.RemoveAll(x => x.TaskId == userTask.TaskId);
                        executableTasks[i] = null;
                    }
                }
            }

            ScheduleTaskOnThread();
        }

        private Task CreateTask(UserTask taskToExecute)
        {
            return Task.Factory.StartNew(() =>
            {
                // wait for specified time
                // Task.Delay(taskToExecute.Duration).Wait();
                while (taskToExecute.ExecutingTime > 0 && taskToExecute.CurrentThreadId != -1)
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    taskToExecute.ExecutingTime -= 100;
                    s.Stop();
                    if (s.ElapsedMilliseconds < 100)
                    {
                        int time = (int)(100 - s.ElapsedMilliseconds);
                        Task.Delay(time).Wait();
                        Console.WriteLine("Task ID: {0}, Thread: {1}", taskToExecute.TaskId, taskToExecute.CurrentThreadId);
                    }
                }

                if (taskToExecute.CurrentThreadId != -1 && !taskToExecute.IsCompleted)
                {
                    taskToExecute.ThreadJob(String.Format(
                            "Task ID: {0}, Task Priority: {1}, Task Duration: {2}, Task Executed On Thread: {3}, Task Resources Count: {4}, Resources: {5}",
                            taskToExecute.TaskId,
                            Priority.GetPriorityString(taskToExecute.Priority),
                            taskToExecute.Duration,
                            taskToExecute.CurrentThreadId,
                            taskToExecute.Resources.Count,
                            String.Join("--", taskToExecute.Resources)
                    ));

                    lock (writer)
                    {
                        writer.WriteLine(
                             "Task ID: {0}, Task Priority: {1}, Task Duration: {2}, Task Executed On Thread: {3}, Task Resources Count: {4}, Resources: {5}",
                             taskToExecute.TaskId,
                             Priority.GetPriorityString(taskToExecute.Priority),
                             taskToExecute.Duration,
                             taskToExecute.CurrentThreadId,
                             taskToExecute.Resources.Count,
                             String.Join("--", taskToExecute.Resources
                        ));

                        writer.Flush();
                    }

                    taskToExecute.Resources.ForEach(r =>
                    {
                        r.Unlock();
                        r.SetTaskId(-1);
                    });

                    taskToExecute.Complete();
                    lock (ScheduleLockObject)
                    {
                        EndTask();
                    }
                }

                // task which ended execution needs to be deleted from the thread 
                // new task needs to be on thread
            });
        }

        private Task CreateTaskUsingFactory(UserTask taskToExecute)
        {
            return taskFactory.StartNew(() =>
            {
                // wait for specified time
                // Task.Delay(taskToExecute.Duration).Wait();
                while (taskToExecute.ExecutingTime > 0 && taskToExecute.CurrentThreadId != -1)
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    taskToExecute.ExecutingTime -= 100;
                    s.Stop();
                    if (s.ElapsedMilliseconds < 100)
                    {
                        int time = (int)(100 - s.ElapsedMilliseconds);
                        Task.Delay(time).Wait();
                    }
                }

                if (taskToExecute.CurrentThreadId != -1 && !taskToExecute.IsCompleted)
                {
                    taskToExecute.ThreadJob(String.Format(
                            "Task ID: {0}, Task Priority: {1}, Task Duration: {2}, Task Executed On Thread: {3}, Task Resources Count: {4}, Resources: {5}",
                            taskToExecute.TaskId,
                            Priority.GetPriorityString(taskToExecute.Priority),
                            taskToExecute.Duration,
                            taskToExecute.CurrentThreadId,
                            taskToExecute.Resources.Count,
                            String.Join("--", taskToExecute.Resources)
                    ));

                    lock (writer)
                    {
                        writer.WriteLine(
                            "Task ID: {0}, Task Priority: {1}, Task Duration: {2}, Task Executed On Thread: {3}, Task Resources Count: {4}, Resources: {5}",
                            taskToExecute.TaskId,
                            Priority.GetPriorityString(taskToExecute.Priority),
                            taskToExecute.Duration,
                            taskToExecute.CurrentThreadId,
                            taskToExecute.Resources.Count,
                            String.Join("--", taskToExecute.Resources
                        ));

                        writer.Flush();
                    }

                    taskToExecute.Resources.ForEach(r =>
                    {
                        r.Unlock();
                        r.SetTaskId(-1);
                    });

                    taskToExecute.Complete();
                    lock (ScheduleLockObject)
                    {
                        EndTask();
                    }
                }

                // task which ended execution needs to be deleted from the thread 
                // new task needs to be on thread
            }, tokenSource.Token);
        }

        private List<Resource> GetRandomResources()
        {
            Random random = new Random();
            int numberOfResources = random.Next(0, 4);
            List<Resource> taskResources = new List<Resource>();
            int[] resIndex = new int[numberOfResources];

            int count = 0;

            if (count != numberOfResources)
                do
                {
                    int index = random.Next(0, Resources.Length);
                    if (!resIndex.Contains(index))
                    {
                        resIndex[count] = index;
                        count++;
                        taskResources.Add(Resources[index]);
                    }

                } while (count < numberOfResources);

            return taskResources;
        }
    }
}
