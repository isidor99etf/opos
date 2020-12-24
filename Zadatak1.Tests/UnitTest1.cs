using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zadatak1.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void NumberOfExecutingTasksTest()
        {
            CustomTaskScheduler t = new CustomTaskScheduler(5, 10_000, true, false);

            t.Schedule(x => x.Trim(), 3, new List<Resource>());
            t.Schedule(x => x.Trim(), 2, new List<Resource>());
            t.Schedule(x => x.Trim(), 1, new List<Resource>());
            t.Schedule(x => x.Trim(), 4, new List<Resource>());
            t.Schedule(x => x.Trim(), 5, new List<Resource>());

            Assert.AreEqual(5, t.NumberOfExecutingTasks);
        }
    }
}
