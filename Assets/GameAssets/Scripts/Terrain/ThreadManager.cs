using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    public static ThreadManager active;
    Queue<ThreadData> threadJobQueue = new Queue<ThreadData>();
    Queue<Action> mainThreadJobQueue = new Queue<Action>();
    private void Awake()
    {
        active = this;
    }
    private void Update()
    {
        for (int i = 0; i < threadJobQueue.Count; i++)
        {
            ThreadData threadData = threadJobQueue.Dequeue();
            threadData.callBack(threadData.result);
        }
        if(mainThreadJobQueue.Count > 0)
        {
            Action action = mainThreadJobQueue.Dequeue();
            action.Invoke();
        }
    }

    public static void AddThreadJob(Func<object> function, Action<object> callBack)
    {
        ThreadStart threadStart = delegate
        {
            active.Thread(function, callBack);
        };
        new Thread(threadStart).Start();
    }
    private void Thread(Func<object> function, Action<object> callBack)
    {
        object result = function();
        lock (threadJobQueue)
        {
            threadJobQueue.Enqueue(new ThreadData(callBack, result));
        }
    }
    public static void AddThreadJob(Action function, Action callBack)
    {
        ThreadStart threadStart = delegate
        {
            active.Thread(function, callBack);
        };
        new Thread(threadStart).Start();
    }
    private void Thread(Action function, Action callBack)
    {
        function.Invoke();
        lock (threadJobQueue)
        {
            mainThreadJobQueue.Enqueue(callBack);
        }
    }
    public static void AddMainThreadJob(Action mainThreadAction)
    {
        active.mainThreadJobQueue.Enqueue(mainThreadAction);
    }
}
public struct ThreadData
{
    public Action<object> callBack;
    public object result;

    public ThreadData(Action<object> callBack, object result)
    {
        this.callBack = callBack;
        this.result = result;
    }
}
