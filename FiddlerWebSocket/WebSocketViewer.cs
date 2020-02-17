//===========================================================================
//
//===========================================================================

namespace FiddlerWebSocket
{
  using Fiddler;

  public class WebSocketViewer : IAutoTamper3
  {
    private readonly Handler handler_;

    public WebSocketViewer()
    {
      Log.Info("Has been initialized");

      handler_ = new Handler();
      FiddlerApplication.OnWebSocketMessage += FiddlerApplicationOnOnWebSocketMessage_;
    }

    public static string Name => nameof(WebSocketViewer);

    public void OnLoad()
    {
    }

    public void OnBeforeUnload()
    {
    }

    public void AutoTamperRequestBefore(Session session)
    {
      var useragent = session.RequestHeaders["User-Agent"];

      if (useragent == Name)
      {
        session["ui-color"] = "orange";
        session.oRequest.FailSession(200, "Fake", "Not sent due to being fake");
      }
    }

    public void AutoTamperRequestAfter(Session session)
    {
    }

    public void AutoTamperResponseBefore(Session session)
    {
    }

    public void AutoTamperResponseAfter(Session session)
    {
    }

    public void OnBeforeReturningError(Session session)
    {
    }

    public void OnPeekAtResponseHeaders(Session session)
    {
    }

    public void OnPeekAtRequestHeaders(Session session)
    {
    }

    private void FiddlerApplicationOnOnWebSocketMessage_(
      object sender,
      WebSocketMessageEventArgs args)
    {
      if (sender is Session session)
      {
        var message = args.oWSM;
        handler_.Enqueue(session, message);
      }
      else
      {
          Log.Info("Unknown sender");
      }
    }
  }
}