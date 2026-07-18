using System;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace Dalamud.RichPresence.Services
{
    internal class IpcService : IDisposable
    {
        // Waitingway IPCs
        private readonly ICallGateSubscriber<int?> wwQueueType = Plugin.PluginInterface.GetIpcSubscriber<int?>("Waitingway.QueueType");
        private readonly ICallGateSubscriber<int?> wwCurrentPosition = Plugin.PluginInterface.GetIpcSubscriber<int?>("Waitingway.CurrentPosition");
        private readonly ICallGateSubscriber<TimeSpan?> wwEstimatedTimeRemaining = Plugin.PluginInterface.GetIpcSubscriber<TimeSpan?>("Waitingway.EstimatedTimeRemaining");

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
