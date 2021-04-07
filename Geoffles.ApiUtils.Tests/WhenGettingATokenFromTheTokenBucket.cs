using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Geoffles.ApiUtils.Tests
{ 
    public class WhenGettingATokenFromTheTokenBucket
    {
        class TestTimeProvider : TokenBucket.ITimeProvider
        {
            private static readonly Task COMPLETED = Task.FromResult(true);

            private DateTime _time = new DateTime(0);
            public int? LastDelay { get; private set; }
            public void AddMillis(int millis)
            {
                _time = _time.AddMilliseconds(millis);
                LastDelay = null;
            }

            public DateTime GetDateTime()
            {
                return _time;
            }
            public Task Delay(int millis, CancellationToken cancellationToken = default(CancellationToken))
            {
                LastDelay = millis;
                return COMPLETED;
            }
        }

        [Fact]
        public async Task ThenTheTokensMustDeplete()
        {
            var testTimeProvider = new TestTimeProvider();
            var bucket = new TokenBucket(1000, 5, testTimeProvider);

            testTimeProvider.AddMillis(4000);
            await bucket.WaitTokenAsync();
            Assert.Equal(3, bucket.Tokens);

            await bucket.WaitTokenAsync();
            Assert.Equal(2, bucket.Tokens);

            await bucket.WaitTokenAsync();
            Assert.Equal(1, bucket.Tokens);
        }

        [Fact]
        public async Task ThenAnEmptyBucketMustCauseADelay()
        {
            var testTimeProvider = new TestTimeProvider();
            var bucket = new TokenBucket(1000, 5, testTimeProvider);
            testTimeProvider.AddMillis(1600);
          
            await bucket.WaitTokenAsync();
            Assert.Null(testTimeProvider.LastDelay);
            
            await bucket.WaitTokenAsync();
            Assert.Equal(400, testTimeProvider.LastDelay);

            testTimeProvider.AddMillis(testTimeProvider.LastDelay.Value);

            await bucket.WaitTokenAsync();
            Assert.Null(testTimeProvider.LastDelay);

        }
    }
}
