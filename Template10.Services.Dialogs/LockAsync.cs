﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Template10.Services.Dialogs
{
    internal class LockAsync : IDisposable
    {
        private static readonly object LockPoint = new object();

        private static readonly Dictionary<object, Queue<TaskCompletionSource<bool>>> LockPointsDictionary =
            new Dictionary<object, Queue<TaskCompletionSource<bool>>>();

        private readonly bool _keepQueueIfEmpty;
        private object _lockPointObject;

        private Queue<TaskCompletionSource<bool>> _lockQueue;
        private bool _wasUnlock;

        private LockAsync(bool keepQueueIfEmpty)
        {
            _keepQueueIfEmpty = keepQueueIfEmpty;
        }

        public Queue<TaskCompletionSource<bool>> Queue => _lockQueue;

        public void Dispose()
        {
            if (!_wasUnlock)
                Unlock();
        }

        public async Task Lock(object lockPointObject)
        {
            _lockPointObject = lockPointObject;
            var waiter = new TaskCompletionSource<bool>();
            lock (LockPoint)
            {
                if (!LockPointsDictionary.TryGetValue(lockPointObject, out _lockQueue))
                {
                    _lockQueue = new Queue<TaskCompletionSource<bool>>();
                    LockPointsDictionary[lockPointObject] = _lockQueue;
                }

                if (_lockQueue.Count == 0)
                    waiter.TrySetResult(true);
                _lockQueue.Enqueue(waiter);
            }
            await waiter.Task;
        }

        public void Unlock()
        {
            _wasUnlock = true;
            NextInQueue();
        }

        private void NextInQueue()
        {
            lock (LockPoint)
            {
                var stop = false;
                do
                {
                    if (_lockQueue.Count > 0)
                    {
                        var result = _lockQueue.Dequeue();
                        stop = result.TrySetResult(true);
                    }
                    else
                    {
                        stop = true;
                    }

                    if (_lockQueue.Count == 0 && !_keepQueueIfEmpty) // clear queue
                        LockPointsDictionary.Remove(_lockPointObject);
                } while (!stop);
            }
        }

        public static async Task<LockAsync> Create(object lockPointObject, bool keepQueueIfEmpty = true)
        {
            var lockAsync = new LockAsync(keepQueueIfEmpty);
            await lockAsync.Lock(lockPointObject);
            return lockAsync;
        }
    }
}