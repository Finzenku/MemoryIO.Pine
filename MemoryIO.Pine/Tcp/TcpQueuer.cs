using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MemoryIO.Pine.Tcp
{
    internal abstract class TcpQueuer<T>
    {
        protected TcpClient client;
        protected NetworkStream networkStream;
        protected CancellationTokenSource cancellationTokenSource;
        protected BlockingCollection<RequestWrapper<T>> requestQueue;
        protected ConcurrentQueue<TaskCompletionSource<T>> responseQueue;

        public TcpQueuer(TcpClient tcpClient)
        {
            client = tcpClient;
            networkStream = client.GetStream();
            if (networkStream.DataAvailable)
                ClearStream();
            cancellationTokenSource = new();
            requestQueue = new();
            responseQueue = new();

            // Start processing requests and responses in separate threads
            ThreadPool.QueueUserWorkItem(ProcessRequests);
            ThreadPool.QueueUserWorkItem(ProcessResponses);
        }

        public async Task ClearStream()
        {
            byte[] buffer = new byte[1024];
            while (networkStream.DataAvailable)
            {
                await networkStream.ReadAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task<T> SendRequestAsync(byte[] requestData)
        {
            var tcs = new TaskCompletionSource<T>();
            var requestWrapper = new RequestWrapper<T>(requestData, tcs);
            requestQueue.Add(requestWrapper);
            return await tcs.Task;
        }

        protected void ProcessRequests(object? state)
        {
            try
            {
                foreach (var requestWrapper in requestQueue.GetConsumingEnumerable(cancellationTokenSource.Token))
                {

                    // Send the request over the network
                    byte[] requestData = requestWrapper.Request;
                    networkStream.Write(requestData, 0, requestData.Length);

                    responseQueue.Enqueue(requestWrapper.TaskCompletionSource);
                }
            }
            catch (OperationCanceledException)
            {
                // Request processing canceled
            }
            finally
            {
                requestQueue.CompleteAdding();
            }
        }
        protected void ProcessResponses(object? state)
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (networkStream.DataAvailable && responseQueue.TryDequeue(out TaskCompletionSource<T>? tcs))
                    {
                        T responseData = ReadResponse();
                        tcs.SetResult(responseData);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Response processing canceled
            }
        }

        protected abstract T ReadResponse();
    }
}
