using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Geoffles.ApiUtils.Tests
{
    public class WhenAcquiringAnAwaitableLock
    {
        [Fact]
        public void ThenTheLockMustBeReleasableOnAnotherThread()
        {
            AwaitableLock l = new AwaitableLock();

            var releaseTask = l.WaitOneAsync();

            Assert.True(l.IsLocked);


            var thread = new Thread(() => {releaseTask.GetAwaiter().GetResult().Dispose(); });
            thread.Start();
            thread.Join();

            Assert.False(l.IsLocked);
        }

        [Fact]
        public void ThenLocksMustQueueUp()
        {
            AwaitableLock l = new AwaitableLock();

            var releaseTask1 = l.WaitOneAsync();
            Assert.True(l.IsLocked);

            var releaseTask2 = l.WaitOneAsync();
            Assert.Equal(TaskStatus.Created, releaseTask2.Status);

            releaseTask1.GetAwaiter().GetResult().Dispose();
            
            Assert.True(l.IsLocked);

            bool isWaitingRunningOrCompleted = releaseTask2.Status == TaskStatus.WaitingForActivation 
                || releaseTask2.Status == TaskStatus.WaitingToRun
                || releaseTask2.Status == TaskStatus.Running
                || releaseTask2.Status == TaskStatus.RanToCompletion;

            Assert.True(isWaitingRunningOrCompleted);

            releaseTask2.GetAwaiter().GetResult().Dispose();

            Assert.False(l.IsLocked);
        }

        [Fact]
        public async void ThenLocksCanGoInUsing()
        {
            var l = new AwaitableLock();
            using ( await l.WaitOneAsync())
            {
                Assert.True(l.IsLocked);
            }

            Assert.False(l.IsLocked);
        }
    }
}
