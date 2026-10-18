using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// High-Performance Coroutine Runner (More Effective Coroutines architecture).
    /// Provides zero-allocation, taggable, segmented coroutines for mobile performance.
    /// Eliminates garbage collection spikes caused by standard Unity WaitForSeconds allocations.
    /// </summary>
    public class EffectiveCoroutineRunner : MonoBehaviour
    {
        public static EffectiveCoroutineRunner Instance { get; private set; }

        public struct CoroutineJob
        {
            public int id;
            public string tag;
            public float executeAtTime;
            public Action callback;
            public Func<bool> predicate;
            public bool isLooping;
            public float interval;
        }

        private static int nextId = 1;
        private readonly List<CoroutineJob> activeJobs = new List<CoroutineJob>(128);
        private readonly List<CoroutineJob> pendingAdditions = new List<CoroutineJob>(32);
        private readonly HashSet<int> cancelledIds = new HashSet<int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            float currentTime = Time.time;

            if (pendingAdditions.Count > 0)
            {
                activeJobs.AddRange(pendingAdditions);
                pendingAdditions.Clear();
            }

            for (int i = activeJobs.Count - 1; i >= 0; i--)
            {
                var job = activeJobs[i];

                if (cancelledIds.Contains(job.id))
                {
                    activeJobs.RemoveAt(i);
                    cancelledIds.Remove(job.id);
                    continue;
                }

                if (currentTime >= job.executeAtTime)
                {
                    job.callback?.Invoke();

                    if (job.isLooping && !cancelledIds.Contains(job.id))
                    {
                        job.executeAtTime = currentTime + job.interval;
                        activeJobs[i] = job;
                    }
                    else
                    {
                        activeJobs.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Schedules a zero-allocation delayed action after delaySeconds.
        /// </summary>
        public static int CallDelayed(float delaySeconds, Action callback, string tag = null)
        {
            EnsureInstance();
            int id = nextId++;
            Instance.pendingAdditions.Add(new CoroutineJob
            {
                id = id,
                tag = tag,
                executeAtTime = Time.time + delaySeconds,
                callback = callback,
                isLooping = false,
                interval = 0f
            });
            return id;
        }

        /// <summary>
        /// Schedules a zero-allocation repeating action at a fixed interval.
        /// </summary>
        public static int CallPeriodically(float intervalSeconds, Action callback, string tag = null)
        {
            EnsureInstance();
            int id = nextId++;
            Instance.pendingAdditions.Add(new CoroutineJob
            {
                id = id,
                tag = tag,
                executeAtTime = Time.time + intervalSeconds,
                callback = callback,
                isLooping = true,
                interval = intervalSeconds
            });
            return id;
        }

        /// <summary>
        /// Cancels a running job by ID.
        /// </summary>
        public static void CancelJob(int jobId)
        {
            if (Instance != null)
            {
                Instance.cancelledIds.Add(jobId);
            }
        }

        /// <summary>
        /// Cancels all jobs with a specific tag.
        /// </summary>
        public static void CancelAllWithTag(string tag)
        {
            if (Instance == null || string.IsNullOrEmpty(tag)) return;
            foreach (var job in Instance.activeJobs)
            {
                if (job.tag == tag) Instance.cancelledIds.Add(job.id);
            }
            foreach (var job in Instance.pendingAdditions)
            {
                if (job.tag == tag) Instance.cancelledIds.Add(job.id);
            }
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- EFFECTIVE COROUTINE RUNNER ---");
                Instance = go.AddComponent<EffectiveCoroutineRunner>();
                DontDestroyOnLoad(go);
            }
        }
    }
}
