using System;
using System.Collections.Generic;
using System.Threading;

namespace EZ_Robot_Unity_DLL {

  /// <summary>
  /// This scheduler ensures tasks are executed on a background threads with queuing. Tasks are added to the queue and when completed, the next task runs.
  /// </summary>
  internal class EZTaskScheduler : IDisposable {

    internal class TaskCls {

      public object Param;
      public int    TaskID;
    }

    private volatile int    _taskId         = 0;
    private volatile string _name           = string.Empty;
    private volatile bool   _cancelRequsted = false;

    public delegate void OnEventErrorEventHandler(int taskId, object o, Exception ex);

    /// <summary>
    /// Raised if the event throws an exeception on the same thread as the event was executed.
    /// </summary>
    public event OnEventErrorEventHandler OnEventError;

    public delegate void OnEventCompletedEventHandler(int taskId, object o);

    /// <summary>
    /// Raised when the event has completed on the same thread as the event executed.
    /// </summary>
    public event OnEventCompletedEventHandler OnEventCompleted;

    public delegate void OnEventStartEventHandler(int taskId, object o);

    /// <summary>
    /// Raised before the event is started on the same thread as the event will execute.
    /// </summary>
    public event OnEventStartEventHandler OnEventStart;

    public delegate void OnEventToRunEventHandler(int taskId, object o);

    /// <summary>
    /// The event/task that will run for every instance.
    /// </summary>
    public event OnEventToRunEventHandler OnEventToRun;


    public delegate void OnQueueCompletedHandler();

    /// <summary>
    /// Raised when all events/tasks in the queue have completed executing. Executes on the same thread that the last task ran on.
    /// </summary>
    public event OnQueueCompletedHandler OnQueueCompleted;

    private List<TaskCls> _tasks = new List<TaskCls>();

    private volatile bool _isRunning = false;

    public int GetTaskCountInQueue {
      get {
        return _tasks.Count;
      }
    }

    public bool IsCancelRequested {
      get {
        return _cancelRequsted;
      }
    }

    public bool IsRunning {
      get {
        return _isRunning;
      }
    }

    public EZTaskScheduler(string name) {

      _name = name;

      System.Diagnostics.Debug.WriteLine("EZ Task Scheduler Initialized: " + name);
    }

    public void CancelCurrentTask() {

      _cancelRequsted = true;
    }

    public void ClearAllQeuedTasks() {

      _tasks.Clear();
    }

    /// <summary>
    /// Add task to the queue.
    /// *Note: This does not start the task scheduler if it's not already running.
    ///        You can use this to "prep" tasks to be executed.
    ///        If you want to add more items to the queue AND run them, use the StartNew()
    /// </summary>
    public int AddToQueue(object param) {

      if (_disposed)
        throw new Exception("Object (EZTaskScheduler) has been disposed");

      if (OnEventToRun == null)
        throw new Exception("Missing the event to execute (OnEventToRun is null)");

      _taskId++;

      _tasks.Add(new TaskCls() {
        TaskID = _taskId,
        Param = param
      });

      return _taskId;
    }

    /// <summary>
    /// This will execute the scheduler to begin processing the items in the queue if the scheduler is not already running
    /// *Note: This is only needed to be called if you prepped the queue with AddToQueue() when the scheduler wasn't already running
    ///        Remember, the scheduler runs when the StartNew() is used. 
    /// </summary>
    public void ProcessItemsInQueue() {

      if (_disposed)
        throw new Exception("Object (EZTaskScheduler) has been disposed");

      if (OnEventToRun == null)
        throw new Exception("Missing the event to execute (OnEventToRun is null)");

      if (!_isRunning) {

        Thread t = new Thread(doWork);
        t.Name = string.Format("{0} - Looper", _name);
        t.IsBackground = true;
        t.Start();
      }
    }

    /// <summary>
    /// Add items to the queue and start the scheduler to begin processing them
    /// *Note: This is the method that you should always be using unless you wish to prep items ahead of time, then use AddtoQueue() and ProcesItemsToQueue(), respectively.
    /// </summary>
    public int StartNew(object param) {

      if (_disposed)
        throw new Exception("Object (EZTaskScheduler) has been disposed");

      if (OnEventToRun == null)
        throw new Exception("Missing the event to execute (OnEventToRun is null)");

      //System.Diagnostics.Debug.WriteLine("Added to Queue: " + _name);

      _taskId++;

      _tasks.Add(new TaskCls() {
        TaskID = _taskId,
        Param = param
      });

      if (!_isRunning) {

        Thread t = new Thread(doWork);
        t.Name = string.Format("{0} - Looper", _name);
        t.IsBackground = true;
        t.Start();
      }

      return _taskId;
    }

    void doWork() {

      if (_isRunning)
        return;

      _isRunning = true;

      //System.Diagnostics.Debug.WriteLine("Looper Started: " + _name);

      try {

        if (_disposed)
          return;

        do {

          _cancelRequsted = false;

          if (_tasks == null)
            return;

          TaskCls tc = _tasks[0];

          _tasks.RemoveAt(0);

          try {

            if (!_disposed && OnEventStart != null)
              OnEventStart(tc.TaskID, tc.Param);

            if (!_disposed)
              OnEventToRun(tc.TaskID, tc.Param);

            if (!_disposed && OnEventCompleted != null)
              OnEventCompleted(tc.TaskID, tc.Param);

          } catch (Exception ex) {

            if (!_disposed && OnEventError != null)
              OnEventError(tc.TaskID, tc.Param, ex);
          }
        } while (!_disposed && _tasks != null && _tasks.Count > 0);
      } finally {

        _isRunning = false;
      }

      if (OnQueueCompleted != null)
        OnQueueCompleted();
    }

    private bool _disposed;

    public void Dispose() {

      Dispose(true);
    }

    private void Dispose(bool disposing) {

      if (disposing && _disposed == false) {

        _disposed = true;

        _cancelRequsted = true;

        System.Diagnostics.Debug.WriteLine("EZ Task Disposing: " + _name);

        _tasks.Clear();

        _tasks = null;
      }

      GC.SuppressFinalize(this);
    }
  }
}