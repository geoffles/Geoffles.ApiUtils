using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    public class NoTokensAvailableException : Exception
    {
        public NoTokensAvailableException() : base("No tokens available to service the request")
        {

        }
    }

    public class TokenBucket
    {
        /// <summary>
        /// Abstracted time provider. Mostly useful for testing.
        /// </summary>
        public interface ITimeProvider
        {
            /// <summary>
            /// Get the current time
            /// </summary>
            /// <returns></returns>
            DateTime GetDateTime();

            /// <summary>
            /// Get a delay task
            /// </summary>
            Task Delay(int millis, CancellationToken cancellationToken);
        }

        private class DefaultTimeProvider : ITimeProvider
        {
            public DateTime GetDateTime() { return DateTime.Now; }
            public Task Delay(int millis, CancellationToken cancellationToken) { return Task.Delay(millis, cancellationToken); }
        }

        public ITimeProvider TimeProvider { get; private set; }

        /// <summary>
        /// Not synchronized. Balance is updated lazily. Call RefreshTokenBalance to get the current balance.
        /// </summary>
        /// <remarks>
        /// Calling refresh balance is synchronised will wait until the current queue is processed.
        /// </remarks>
        public int Tokens { get; private set; }
        public int TokenPeriodMillis { get; private set; }
        public int MaxTokens { get; private set; }

        private DateTime _lastToken;
        private readonly AwaitableLock _lock = new AwaitableLock();

        /// <summary>
        /// Creates a new bucket with a token period and capacity.
        /// </summary>
        public TokenBucket(int tokenPeriodMillis, int maxTokens): this (tokenPeriodMillis, maxTokens, new DefaultTimeProvider())
        {
        }

        /// <summary>
        /// Sets an alternative time provider
        /// </summary>
        public TokenBucket(int tokenPeriodMillis, int maxTokens, ITimeProvider timeProvider)
        {
            TimeProvider = timeProvider;
            TokenPeriodMillis = tokenPeriodMillis;
            MaxTokens = maxTokens;
            _lastToken = timeProvider.GetDateTime();
            
        }

        /// <summary>
        /// Updates the current token balance. This method is task synchronised.
        /// </summary>
        /// <returns></returns>
        public async Task RefreshTokenBalance()
        {
            using (var @lock = await _lock.WaitOneAsync())
            {
                CalculateTokenBalanceAndMillisTillNext(@lock);
            }
        }

        private int CalculateTokenBalanceAndMillisTillNext(AwaitableLock.ILockRelease @lock)
        {
            if (@lock == null)
            {
                throw new Exception("Token balance must be updated within a lock");
            }

            var now = TimeProvider.GetDateTime();
            var millisSince = (int)now.Subtract(_lastToken).TotalMilliseconds;
            
            var periods = millisSince / TokenPeriodMillis;
            var newTokens = periods + Tokens > MaxTokens ? MaxTokens : periods + Tokens;
            Tokens = newTokens;

            var millisToNext = millisSince % TokenPeriodMillis;

            _lastToken = now.AddMilliseconds(millisToNext - TokenPeriodMillis);
            return millisToNext;
        }

        public void Acquire()
        {            
            using (var lockRelease = _lock.Acquire())
            {
                if (!DoAcquire(lockRelease))
                {
                    throw new NoTokensAvailableException();
                }
            }
        }

        public bool TryAcquire()
        {
            AwaitableLock.ILockRelease lockRelease;
            if (_lock.TryAcquire(out lockRelease))
            {
                using (lockRelease)
                {
                    return DoAcquire(lockRelease);
                }
            }
            else
            {
                return false;
            }
        }

        private bool DoAcquire(AwaitableLock.ILockRelease lockRelease)
        {
            if (Tokens > 0)
            {
                Tokens -= 1;
                return true;
            }

            CalculateTokenBalanceAndMillisTillNext(lockRelease);

            if (Tokens > 0)
            {
                Tokens -= 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a task that will start once a token is acquired. This method is task synchronised.
        /// </summary>
        /// <returns></returns>
        public async Task WaitTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {

            using (var @lock = await _lock.WaitOneAsync(cancellationToken))
            { 
                if (Tokens > 0)
                {
                    Tokens -= 1;
                    return;
                }

                var waitMillis = CalculateTokenBalanceAndMillisTillNext(@lock);

                if (Tokens > 0)
                {
                    Tokens -= 1;
                    return;
                }
                else if (waitMillis == 0)
                {
                    return;
                }
                else
                {
                    Tokens -= 1;
                    await TimeProvider.Delay(waitMillis, cancellationToken);
                    _lastToken = _lastToken.AddMilliseconds(TokenPeriodMillis);
                    return;
                }
            }
        }
    }
}
