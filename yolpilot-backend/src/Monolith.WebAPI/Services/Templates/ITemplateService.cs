using System.Collections.Generic;
using System.Threading.Tasks;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Templates
{
    public interface ITemplateService
    {
        Task<MessageTemplate> GetTemplateAsync(int workspaceId, TemplateType type, TemplateChannel channel);
        Task<List<MessageTemplate>> GetWorkspaceTemplatesAsync(int workspaceId);
        Task<MessageTemplate> CreateTemplateAsync(MessageTemplate template);
        Task<MessageTemplate> UpdateTemplateAsync(MessageTemplate template);
        Task<bool> DeleteTemplateAsync(int templateId, int workspaceId);
        Task<string> ProcessTemplateAsync(string templateBody, Dictionary<string, object> data);
        Task<(string subject, string body)> GetProcessedEmailContentAsync(
            int workspaceId, 
            TemplateType type, 
            Dictionary<string, object> data);
        Task<string> GetProcessedWhatsAppContentAsync(
            int workspaceId, 
            TemplateType type, 
            Dictionary<string, object> data);
    }
}