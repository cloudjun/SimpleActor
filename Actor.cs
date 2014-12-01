using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleActor
{
    // Have to have this class instead of just a enum for volatilewrite()
    public class ActorContext
    {
        public const int AVAILABLE = 0;
        public const int EXECUTING = 1;
        public const int EXITED = 2;

        public int Status;
    }

    public interface IActor
    {
        // count of messages waiting to be processed (queue size)
        long MessageCount { get; }
        
        void Execute();

        bool Exited { get; }

        ActorContext Context { get; }
    }

    public class GateKeeper
    {
        public static void ReadyToExecute(IActor actor)
        {
            if (actor.Exited)
            {
                return;
            }

            var actorStatus = Interlocked.CompareExchange(
                ref actor.Context.Status,
                ActorContext.EXECUTING,
                ActorContext.AVAILABLE);
            if (actorStatus == ActorContext.AVAILABLE)
            {
                Task.Factory.StartNew(() => GateKeeper.Execute(actor));
            }
        }

        public static void Execute(IActor actor)
        {
            actor.Execute();
            if (actor.Exited)
            {
                Thread.VolatileWrite(ref actor.Context.Status, ActorContext.EXITED);
                return;
            }

            Thread.VolatileWrite(ref actor.Context.Status, ActorContext.AVAILABLE);

            if (actor.MessageCount > 0)
            {
                ReadyToExecute(actor);
            }
        }

        public static void Log(string message)
        {
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId + "-" + message);
        }
    }

    public abstract class Actor<T> : IActor
    {
        public long MessageCount
        {
            get { return _queue.Count; }
        }

        private bool _exited;
        public bool Exited
        {
            get { return _exited; }
        }

        private ActorContext _context;
        ActorContext IActor.Context
        {
            get { return _context; }
        }

        private readonly ConcurrentQueue<T> _queue;

        protected Actor()
        {
            _exited = false;
            _context = new ActorContext();
            _queue = new ConcurrentQueue<T>();
        }

        /// <summary>
        /// Only to be called by the GateKeeper.
        /// </summary>
        public void Execute()
        {
            // grab a work item from the queue
            T item;
            if (!_queue.TryDequeue(out item)) return;

            DoWork(item);
        }

        /// <summary>
        /// For the client to give a work item to the actor.
        /// </summary>
        /// <param name="item"></param>
        public void AddWorkItem(T item)
        {
            _queue.Enqueue(item);

            GateKeeper.ReadyToExecute(this);
        }

        public void Exit()
        {
            _exited = true;
        }

        /// <summary>
        /// The real work that is done by this actor.
        /// </summary>
        /// <param name="item"></param>
        public abstract void DoWork(T item);
    }
}