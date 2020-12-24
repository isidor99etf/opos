using System;
using System.Collections.Generic;
using System.Text;

namespace Zadatak1
{
    public class Resource
    {

        public String Name { get; private set; }
        public bool IsLocked { get; private set; }

        public int TaskId { get; private set; }

        public Resource(String name)
        {
            Name = name;
            IsLocked = false;
            TaskId = -1;
        }

        public void Lock() => IsLocked = true;

        public void Unlock() => IsLocked = false;

        public void SetTaskId(int taskId) => TaskId = taskId;

        public override string ToString()
        {
            return Name;
        }
    }
}
