using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleSummoner.Core
{
    /// <summary>
    /// UnityThreadDispatcher manages asynchronous HTTP callbacks, ElevenLabs TTS audio streaming chunks,
    /// and background LLM inference threads by dispatching execution onto Unity's main thread.
    /// Ported directly from Divinatius/BattleSummoner architecture.
    /// </summary>
    public class UnityThreadDispatcher : MonoBehaviour
    {
        private static UnityThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> ExecutionQueue = new ConcurrentQueue<Action>();
        private static readonly object LockObject = new object();
        private static volatile bool isInitialized = false;

        public static UnityThreadDispatcher Instance
        {
            get
            {
                EnsureInitialized();
                return _instance;
            }
        }

        public static void EnsureInitialized()
        {
            if (isInitialized && _instance != null) return;

            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UnityThreadDispatcher>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("UnityThreadDispatcher");
                        _instance = obj.AddComponent<UnityThreadDispatcher>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(obj);
                        }
                    }
                }
                isInitialized = true;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                isInitialized = true;
                if (transform.parent == null && Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Drain the concurrent queue on Unity's main thread
            while (ExecutionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityThreadDispatcher] Exception executing dispatched main thread action: {ex}");
                }
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the Unity main thread during the next Update tick.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            EnsureInitialized();
            ExecutionQueue.Enqueue(action);
        }

        /// <summary>
        /// Enqueues a coroutine to be started on the main thread from an asynchronous thread.
        /// </summary>
        public static void Enqueue(IEnumerator coroutine)
        {
            if (coroutine == null) return;
            Enqueue(() =>
            {
                if (Instance != null)
                {
                    Instance.StartCoroutine(coroutine);
                }
            });
        }

        /// <summary>
        /// Dispatches an action onto the main thread and returns an awaitable Task.
        /// </summary>
        public static Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(() =>
            {
                try
                {
                    action?.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Dispatches a function returning a result onto the main thread and returns an awaitable Task.
        /// </summary>
        public static Task<T> EnqueueAsync<T>(Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();
            Enqueue(() =>
            {
                try
                {
                    T result = function != null ? function() : default;
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                isInitialized = false;
            }
        }
    }
}
