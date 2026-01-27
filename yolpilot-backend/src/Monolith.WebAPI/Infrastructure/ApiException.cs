namespace Monolith.WebAPI.Infrastructure;

public class ApiException(string message, int? statusCode = null, int? code = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
    public int? Code { get; } = code;
}

public class UnauthorizedException(string message = null) : ApiException(
    message ?? "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.", 401, 401);

public class ForbiddenException(string message = null) : ApiException(
    message ?? "Bu işlem için yetkiniz yok.", 403, 403);

public class NotFoundException(string message = null) : ApiException(
    message ?? "Aradığınız kayıt bulunamadı.", 404, 404);

public class BadRequestException(string message = null) : ApiException(
    message ?? "Geçersiz istek. Lütfen bilgileri kontrol edin.", 400, 400);