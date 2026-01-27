using System.Text;

namespace Monolith.WebAPI.Data;

public class Log : BaseEntity
{
    public Log()
    {
    }

    public Log(Exception exception)
    {
        Message = exception.Message;
        StackTrace = exception.StackTrace;
        Route = null;
        Method = null;
        Query = null;
        Body = null;
    }

    public Log(Exception exception, HttpContext context)
    {
        var requestBody = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            var buffer = new byte[(int) context.Request.ContentLength];
            _ = context.Request.Body.Read(buffer);

            requestBody = Encoding.UTF8.GetString(buffer);

            context.Request.Body.Position = 0;
        }

        Message = exception.Message;
        StackTrace = exception.StackTrace;
        Route = context.Request.Path;
        Method = context.Request.Method;
        Query = context.Request.QueryString.ToString();
        Body = requestBody;
    }

    public string Message { get; private set; }
    public string Route { get; private set; }
    public string Method { get; private set; }
    public string Query { get; private set; }
    public string Body { get; private set; }
    public string StackTrace { get; private set; }
}