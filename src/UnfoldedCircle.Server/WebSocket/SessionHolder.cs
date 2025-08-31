using System.Collections.Concurrent;

namespace UnfoldedCircle.Server.WebSocket;

internal static class SessionHolder
{
    public static readonly ConcurrentDictionary<string, bool> SubscribeEventsMap = new(StringComparer.Ordinal);
    public static readonly ConcurrentDictionary<string, byte> BroadcastingEvents = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, SetupStep> NextSetupSteps = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, string> ReconfigureEntityMap = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, CancellationTokenSource> CurrentRepeatCommandMap = new(StringComparer.OrdinalIgnoreCase);
}