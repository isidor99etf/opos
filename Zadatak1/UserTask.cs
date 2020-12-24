using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Zadatak1
{
    public class UserTask
    {
        public delegate void Job(String message);

        public int TaskId { get; private set; }

        public Job ThreadJob { get; private set; }

        public int CurrentThreadId { get; set; }

        public int Duration { get; private set; }

        public int ExecutingTime { get; set; }

        public int Priority { get; private set; }

        public bool IsCompleted { get; private set; }

        public List<Resource> Resources { get; private set; }

        public UserTask(Job job, int priority, int duration, List<Resource> resources)
        {
            TaskId = GetRandomId();
            ThreadJob = job;
            Priority = priority;
            Duration = ExecutingTime = duration;
            Resources = resources;
            IsCompleted = false;

            CurrentThreadId = -1;
        }

        /*public UserTask(Job job, int priority, int duration, int currentThreadId)
        {
            TaskId = GetRandomId();
            ThreadJob = job;
            Priority = priority;
            Duration = duration;
            CurrentThreadId = currentThreadId;
            IsCompleted = false;
        }*/

        public void Complete() => IsCompleted = true;
        public void ChangePriority(int priority) => Priority = priority;

        private static List<int> ids = new List<int>();

        private int GetRandomId()
        {
            Random random = new Random();
            int id;
            do
            {
                id = random.Next(100_000, 1_000_000);
            } while (ids.Contains(id));

            ids.Add(id);

            return id;
        }
    }
}
