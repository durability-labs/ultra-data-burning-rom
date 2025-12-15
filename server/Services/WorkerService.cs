namespace UltraDataBurningROM.Server.Services
{
    public interface IWorkerService
    {
        void Attach<T>(Func<IWorkHandler<T>> createHandler) where T : DbEntity;
        void LateStart();
        void Stop();
    }

    public interface IWorkHandler<T> where T : DbEntity
    {
        void Initialize();
        void OnEntity(T entity);
        void Finish();
    }

    public class WorkerService : IWorkerService
    {
        private readonly TimeSpan UpdateFrequency = TimeSpan.FromMinutes(30.0);
        private readonly ILogger<WorkerService> logger;
        private readonly IDatabaseService dbService;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Lock _lock = new Lock();
        private readonly Dictionary<string, IJob> jobs = new Dictionary<string, IJob>();
        private Task worker = Task.CompletedTask;

        public WorkerService(ILogger<WorkerService> logger, IDatabaseService dbService)
        {
            this.logger = logger;
            this.dbService = dbService;
        }

        public void LateStart()
        {
            Log("Starting worker service...");
            worker = StartWorker();
        }

        public void Attach<T>(Func<IWorkHandler<T>> createHandler) where T : DbEntity
        {
            var typename = typeof(T).Name;
            Log("Worker service is attaching: " + typename);
            lock (_lock)
            {
                if (jobs.TryGetValue(typename, out IJob? job))
                {
                    var typed = (Job<T>)job;
                    typed.AddHandlerCreator(createHandler);
                }
                else
                {
                    var typed = new Job<T>(dbService);
                    typed.AddHandlerCreator(createHandler);
                    jobs.Add(typename, typed);
                }
            }
        }

        public void Stop()
        {
            Log("Stopping worker service...");
            cts.Cancel();
            worker.Wait();
        }

        private Task StartWorker()
        {
            return Task.Run(() =>
            {
                try
                {
                    Worker();
                }
                catch (Exception ex)
                {
                    Log("Exception in worker: " + ex);
                }
            });
        }

        private void Worker()
        {
            UpdateAll();

            while (!cts.IsCancellationRequested)
            {
                if (!cts.Token.WaitHandle.WaitOne(UpdateFrequency))
                {
                    UpdateAll();
                }
            }
        }

        private void UpdateAll()
        {
            var todo = Array.Empty<IJob>();
            lock (_lock)
            {
                todo = jobs.Values.ToArray();
            }
            foreach (var t in todo) t.Run();
        }

        private void Log(string msg)
        {
            logger.LogInformation(msg);
        }

        private interface IJob
        {
            void Run();
        }

        private class Job<T> : IJob where T : DbEntity
        {
            private readonly IDatabaseService dbService;
            private readonly List<Func<IWorkHandler<T>>> listeners = new List<Func<IWorkHandler<T>>>();

            public Job(IDatabaseService dbService)
            {
                this.dbService = dbService;
            }

            public void AddHandlerCreator(Func<IWorkHandler<T>> onEntity)
            {
                listeners.Add(onEntity);
            }

            public void Run()
            {
                var handlers = listeners.Select(l => l()).ToArray();

                foreach (var h in handlers) h.Initialize();

                dbService.Iterate<T>(entity =>
                {
                    foreach (var h in handlers) h.OnEntity(entity);
                });

                foreach (var h in handlers) h.Finish();
            }
        }
    }
}
