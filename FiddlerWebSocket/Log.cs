//===========================================================================
//
//===========================================================================

namespace FiddlerWebSocket
{
  using Fiddler;

  internal static class Log
  {
    public static void Info(string message)
    {
      FiddlerApplication.Log.LogString($"{WebSocketViewer.Name}: {message}");
    }

    public static void Error(string message)
    {
      Info($"Error: {message}");
    }
  }
}