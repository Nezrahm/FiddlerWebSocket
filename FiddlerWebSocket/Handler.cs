//===========================================================================
//
//===========================================================================

namespace FiddlerWebSocket
{
  using System;
  using System.Collections.Concurrent;
  using System.Linq;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Web;
  using Fiddler;

  internal class Handler : IDisposable
  {
    private static readonly string[] filtered_ =
    {
      ".github.com",
      ".slack.com",
    };

    private readonly ConcurrentQueue<Item> items_ = new ConcurrentQueue<Item>();
    private readonly CancellationTokenSource token_;

    public Handler()
    {
      token_ = new CancellationTokenSource();
      Task.Run(DoProcess_, token_.Token);
    }

    public void Dispose()
    {
      token_?.Cancel();
      token_?.Dispose();
    }

    public void Enqueue(Session session, WebSocketMessage message)
    {
      items_.Enqueue(new Item(session, message));
    }

    private static void SendRequest_(string url, StringBuilder message)
    {
      var capacity = message.Length + url.Length + 130;

      var request = new StringBuilder(capacity)
        .AppendLine($"POST {url} HTTP/1.1")
        .AppendLine($"User-Agent: {WebSocketViewer.Name}")
        .AppendLine("Content-Type: application/json; charset=utf-8")
        .AppendLine($"Content-Length: {message.Length}")
        .AppendLine()
        .Append(message);

      FiddlerApplication.oProxy.SendRequest(request.ToString(), null);
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

    private static bool ShouldProcessMessage_(Item item)
    {
      var host = item.Session.hostname;
      if (filtered_.Any(f => host.EndsWith(f)))
        return false;

      var message = item.Message;
      if (message.FrameType == WebSocketFrameTypes.Text && message.PayloadAsString() == "{}")
        return false;

      return true;
    }

    private static void ProcessMessage_(Session session, WebSocketMessage message)
    {
      var uri = new Uri(session.fullUrl);
      var query = HttpUtility.ParseQueryString(uri.Query);

      var url = GetUrl_(session, message);
      var payload = message.ToString();

      var request = new StringBuilder(payload.Length + 200);
      request.AppendLine(payload);
      request.AppendLine($"IsFinalFrame: {message.IsFinalFrame}");
      request.AppendLine($"clientProtocol: {query["clientProtocol"]}");
      request.AppendLine($"connectionData: {query["connectionData"]}");

      SendRequest_(url, request);
    }

    private async Task DoProcess_()
    {
      while (!token_.IsCancellationRequested)
      {
        if (!items_.TryDequeue(out var item))
        {
          await Task.Delay(100);
          continue;
        }

        if (!ShouldProcessMessage_(item))
          continue;

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