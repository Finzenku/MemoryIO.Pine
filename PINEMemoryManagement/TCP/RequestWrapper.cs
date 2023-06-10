namespace Pine.TCP
{
    internal class RequestWrapper<T>
    {
        public byte[] Request { get; }
        public TaskCompletionSource<T> TaskCompletionSource { get; }

        public RequestWrapper(byte[] request, TaskCompletionSource<T> taskCompletionSource)
        {
            Request = request;
            TaskCompletionSource = taskCompletionSource;
        }
    }
}