// src/Monolith.WebAPI/Services/WhatsApp/IWhatsAppService.cs

namespace Monolith.WebAPI.Services.WhatsApp;

public interface IWhatsAppService
{
    // Tüm metodlara Workspace parametresi eklendi - tam namespace path ile
    Task<bool> SendJourneyStartedMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber, string customerName, string driverName, string estimatedTime);
    Task<bool> SendApproachingMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber, string customerName, string minTime, string maxTime, string driverName = null, string driverPhone = null);
    Task<bool> SendDeliveryCompletedMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber, string customerName, string trackingUrl, string receiverName, string feedbackUrl);
    Task<bool> SendDeliveryFailedMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber, string customerName, string failureReason, string trackingUrl, string feedbackUrl);
    Task<bool> VerifyPhoneNumber(string phoneNumber);
    Task<bool> SendTestMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber);
    string FormatPhoneNumber(string phoneNumber);
}