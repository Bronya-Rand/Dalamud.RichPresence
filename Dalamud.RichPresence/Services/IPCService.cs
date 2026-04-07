using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using System;

namespace Dalamud.RichPresence.Services
{
    internal class IPCService : IDisposable
    {
        // Waitingway IPCs
        private readonly ICallGateSubscriber<int?> wwQueueType;
        private readonly ICallGateSubscriber<int?> wwCurrentPosition;
        private readonly ICallGateSubscriber<TimeSpan?> wwEstimatedTimeRemaining;

        public IPCService()
        {
            wwQueueType = Plugin.PluginInterface.GetIpcSubscriber<int?>("Waitingway.QueueType");
            wwCurrentPosition = Plugin.PluginInterface.GetIpcSubscriber<int?>("Waitingway.CurrentPosition");
            wwEstimatedTimeRemaining =
                Plugin.PluginInterface.GetIpcSubscriber<TimeSpan?>("Waitingway.EstimatedTimeRemaining");
        }

        public bool IsInLoginQueue()
        {
            try
            {
                // We only care about login queues
                return wwQueueType.InvokeFunc() == 1;
            }
            catch (IpcNotReadyError)
            {
                return false;
            }
        }
        public int GetQueuePosition()
        {
            try
            {
                return wwCurrentPosition.InvokeFunc() ?? -1;
            }
            catch (IpcNotReadyError)
            {
                return -1;
            }
        }
        public TimeSpan? GetQueueEstimate()
        {
            try
            {
                return wwEstimatedTimeRemaining.InvokeFunc();
            }
            catch (IpcNotReadyError)
            {
                return null;
            }
        }
        public void Dispose() => GC.SuppressFinalize(this);
    }
}
