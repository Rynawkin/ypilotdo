using FluentValidation;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Monolith.WebAPI.Infrastructure;

public class CustomExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<CustomExceptionHandlerMiddleware> logger, IWebHostEnvironment env)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            // Türkçe kullanıcı dostu mesaj
            var userFriendlyMessage = "Girilen bilgiler geçersiz. Lütfen kontrol ederek tekrar deneyin.";

            // SECURITY: Only expose validation errors in development
            var response = new
            {
                message = userFriendlyMessage,
                errors = env.IsDevelopment() ? exception.Errors : null,
                code = 400
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (ApiException exception)
        {
            context.Response.StatusCode = exception.StatusCode ?? 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = exception.Message,
                code = exception.Code ?? exception.StatusCode ?? 400
            }));
        }
        catch (TaskCanceledException)
        {
            context.Response.StatusCode = 408;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.",
                code = 408
            }));
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = 408;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "İşlem iptal edildi. Lütfen tekrar deneyin.",
                code = 408
            }));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception.");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                code = 500
            }));

            // todo var log = new Log(e, context);
            // await appDbContext.Logs.AddAsync(log);
            // await appDbContext.SaveChangesAsync();
        }
    }
}