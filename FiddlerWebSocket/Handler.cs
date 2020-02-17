//===========================================================================
//
//===========================================================================

namespace FiddlerWebSocket
{
  using System;
  using System.Collections.Concurrent;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Web;

  using Fiddler;

  internal class Handler : IDisposable
  {
    private readonly ConcurrentQueue<Item> items_ = new ConcurrentQueue<Item>();
    private readonly CancellationTokenSource token_;

    public Handler()
    {
      token_ = new CancellationTokenSource();
      var task = new Task(DoProcess_, token_.Token);
      task.Start();
    }

    public void Dispose()
    {
      token_?.Dispose();
    }

    public void Enqueue(Session session, WebSocketMessage message)
    {
      items_.Enqueue(new Item(session, message));
    }

    private static void SendRequest_(string url, string message)
    {
      var request = string.Format(
        "POST {1} HTTP/1.1\n" +
        "User-Agent: {0}\n" +
        "Content-Type: application/json; charset=utf-8\n" +
        "Content-Length: {2}\n" +
        "\n" +
        "{3}",
        WebSocketViewer.Name,
        url,
        message.Length,
        message);

      FiddlerApplication.oProxy.SendRequest(request, null);
    }

    private static string GetUrl_(Session session, WebSocketMessage message)
    {
      return string.Format(
        "http://{0}/Session {1}.{3} - {2}",
        session.host,
        session.id,
        message.IsOutbound ? "Client" : "Server",
        message.ID);
    }

    private static void ProcessMessage_(Session session, WebSocketMessage message)
    {
      if (message.FrameType == WebSocketFrameTypes.Text && message.PayloadAsString() == "{}")
      {
        return;
      }

      var uri = new Uri(session.fullUrl);
      var query = HttpUtility.ParseQueryString(uri.Query);

      var url = GetUrl_(session, message);

      var request = new StringBuilder();
      request.AppendLine(message.ToString());
      request.AppendLine($"IsFinalFrame: {message.IsFinalFrame}");
      request.AppendLine($"clientProtocol: {query["clientProtocol"]}");
      request.AppendLine($"connectionData: {query["connectionData"]}");

      SendRequest_(url, request.ToString());
    }

    private void DoProcess_()
    {
      while (!token_.IsCancellationRequested)
      {
        if (!items_.TryDequeue(out var item))
        {
          Thread.Sleep(100);
          continue;
        }

        try
        {
          ProcessMessage_(item.Session, item.Message);
        }
        catch (Exception e)
        {
          Log.Error(e.ToString());
        }
      }
    }

    private class Item
    {
      public Item(Session session, WebSocketMessage message)
      {
        Session = session;
        Message = message;
      }

      public Session Session { get; }

      public WebSocketMessage Message { get; }
    }
  }
}