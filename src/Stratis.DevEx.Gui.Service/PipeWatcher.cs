namespace H.Pipes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Timers;
  
    using Timer = System.Timers.Timer;

    // ReSharper disable UnusedMember.Global

    public sealed partial class PipeWatcher : IDisposable
    {
        #region Properties

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="PipeWatcher"/> is active
        /// </summary>
        public bool IsStarted => Timer.Enabled;

        private Timer Timer { get; }

        private IReadOnlyCollection<string> LastPipes { get; set; } = new List<string>();

        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of watcher <br/>
        /// Default interval is <see langword="100 milliseconds"/>
        /// </summary>
        /// <param name="interval"></param>
        public PipeWatcher(TimeSpan? interval = default)
        {
            Timer = new Timer((interval ?? TimeSpan.FromMilliseconds(100)).TotalMilliseconds);
            Timer.Elapsed += OnElapsed;
        }

        #endregion

        #region Event Handlers

        private void OnElapsed(object? sender, ElapsedEventArgs args)
        {
            try
            {
                var pipes = GetActivePipes();

                foreach (var name in pipes.Except(LastPipes))
                {
                    _ = OnCreated(name);
                }
                foreach (var name in LastPipes.Except(pipes))
                {
                    _ = OnDeleted(name);
                }

                LastPipes = pipes;
            }
            catch (Exception exception)
            {
                _ = OnExceptionOccurred(exception);
            }
        }

        private global::H.Pipes.PipeWatcher.CreatedEventArgs OnCreated(global::H.Pipes.PipeWatcher.CreatedEventArgs args)
        {
            Created?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the Created event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.CreatedEventArgs OnCreated(
            string name)
        {
            var args = new global::H.Pipes.PipeWatcher.CreatedEventArgs(name);
            Created?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the Deleted event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.DeletedEventArgs OnDeleted(global::H.Pipes.PipeWatcher.DeletedEventArgs args)
        {
            Deleted?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the Deleted event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.DeletedEventArgs OnDeleted(
            string name)
        {
            var args = new global::H.Pipes.PipeWatcher.DeletedEventArgs(name);
            Deleted?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs OnExceptionOccurred(global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs args)
        {
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs OnExceptionOccurred(
            global::System.Exception exception)
        {
            var args = new global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs(exception);
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Starts watching
        /// </summary>
        public void Start()
        {
            LastPipes = GetActivePipes();
            Timer.Start();
        }

        /// <summary>
        /// Stops watching(without disposing) <br/>
        /// You can call <see cref="Start()"/> again if it is required
        /// </summary>
        public void Stop()
        {
            Timer.Stop();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal <see cref="Timer"/>
        /// </summary>
        public void Dispose()
        {
            Timer.Dispose();
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Checks if a given pipe name exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsExists(string name)
        {
            return GetActivePipes().Contains(name);
        }

        /// <summary>
        /// Returns list of active pipes
        /// </summary>
        public static IReadOnlyCollection<string> GetActivePipes()
        {
            return Directory
                .EnumerateFiles(@"\\.\pipe\")
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            .Select(static path => path.Replace(@"\\.\pipe\", string.Empty, StringComparison.Ordinal))
#else
            .Select(static path => path.Replace(@"\\.\pipe\", string.Empty))
#endif
            .ToList();
        }

        /// <summary>
        /// Create new instance of watcher and start it<br/>
        /// Default interval is <see langword="100 milliseconds"/>
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static PipeWatcher CreateAndStart(TimeSpan? interval = default)
        {
            var watcher = new PipeWatcher(interval);
            watcher.Start();

            return watcher;
        }

        #endregion

        #region Events
        /// <summary>
        /// When any pipe created.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.CreatedEventArgs>? Created;

        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.DeletedEventArgs>? Deleted;

        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs>? ExceptionOccurred;

        /// <summary>
        /// 
        /// </summary>
        public class CreatedEventArgs : global::System.EventArgs
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 
            /// </summary>
            public CreatedEventArgs(string name)
            {
                Name = name;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Deconstruct(out string name)
            {
                name = Name;
            }

            /// <summary>
            /// 
            /// </summary>
            public override string ToString()
            {
                return $"(Name={Name})";
            }
        }

        public class DeletedEventArgs : global::System.EventArgs
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 
            /// </summary>
            public DeletedEventArgs(string name)
            {
                Name = name;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Deconstruct(out string name)
            {
                name = Name;
            }

            /// <summary>
            /// 
            /// </summary>
            public override string ToString()
            {
                return $"(Name={Name})";
            }
        }

        public class ExceptionOccurredEventArgs : global::System.EventArgs
        {
            /// <summary>
            /// 
            /// </summary>
            public Exception Exception { get; }

            /// <summary>
            /// 
            /// </summary>
            public ExceptionOccurredEventArgs(Exception exception)
            {
                Exception = exception;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Deconstruct(out Exception exception)
            {
                exception = Exception;
            }

            /// <summary>
            /// 
            /// </summary>
            public override string ToString()
            {
                return $"(Excrption={Exception})";
            }
        }
        #endregion

    }
}
