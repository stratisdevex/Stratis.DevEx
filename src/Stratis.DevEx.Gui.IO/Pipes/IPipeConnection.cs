﻿namespace Stratis.DevEx.Pipes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Stratis.DevEx.Pipes.Formatters;
    using Stratis.DevEx.Pipes.Args;

    /// <summary>
    /// Base class of all connections
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public interface IPipeConnection<T> : IDisposable
    {
        #region Properties

        /// <summary>
        /// Used formatter
        /// </summary>
        public IFormatter Formatter { get; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a message is received.
        /// </summary>
        event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        #endregion

        #region Methods

        /// <summary>
        /// Sends a message over a named pipe. <br/>
        /// </summary>
        /// <param name="value">Message to send</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidOperationException"></exception>
        Task WriteAsync(T value, CancellationToken cancellationToken = default);

        #endregion
    }
}
