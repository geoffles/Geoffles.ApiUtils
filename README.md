Geoffles.ApiUtils
=================

A library with utilities that are helpful when building APIs.

Included is:

-  `AwaitableLock` - A lock that can safely be used by async await. A different thread may release the lock than the one that acquired it.
-  `TokenBucket` - A simple, non-threaded, awaitable token bucket.
-  `CachedHttpHandler` - An caching  handler for `HTTPClient` whose caching bahaviour can be provided
-  `InMemoryContentCacheProvider` - In memory cache provider
-  `FileSystemContentCacheProvider` - On disk cache provider
-  `ThrottledConcurrencyHttpHandler` - A throttling handler for `HTTPClient` that does both rate and concurrency limiting with burst mode.

Awaitable Lock
--------------

This lock can be safely used with async-await as a different thread to the one that acquired it may release it.

Locks are intended to be released by `using` blocks (i.e. it calls dispose on the lock result).

Wait forever for lock:
```
var l = new AwaitableLock();
using ( await l.WaitOneAsync())
{
    //Do something under lock
}
```

Fail on failure to acquire lock (throws exception):
```
var l = new AwaitableLock();
using(l.Acquire())
{
    //Do something under lock
};
```

Soft fail:
```
ILockRelease r;
var l = new AwaitableLock();

if (l.TryAcquire(out r))
{
    using(r)
    {
        //Do something under lock
    }
}
else
{
    //try again later?
}
```


