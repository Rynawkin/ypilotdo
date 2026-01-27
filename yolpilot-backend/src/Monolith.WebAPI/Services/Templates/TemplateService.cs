using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Templates
{
    public class TemplateService : ITemplateService
    {
        private readonly AppDbContext _context;

        public TemplateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MessageTemplate> GetTemplateAsync(int workspaceId, TemplateType type, TemplateChannel channel)
        {
            var template = await _context.MessageTemplates
                .Where(t => t.WorkspaceId == workspaceId 
                    && t.TemplateType == type.ToString() 
                    && t.Channel == channel.ToString()
                    && t.IsActive)
                .OrderByDescending(t => !t.IsDefault) // Önce custom, sonra default
                .FirstOrDefaultAsync();

            // Eğer workspace'e özel template yoksa, default template'i getir
            if (template == null)
            {
                template = await _context.MessageTemplates
                    .Where(t => t.WorkspaceId == workspaceId 
                        && t.TemplateType == type.ToString() 
                        && t.Channel == channel.ToString()
                        && t.IsDefault)
                    .FirstOrDefaultAsync();
            }

            return template;
        }

        public async Task<List<MessageTemplate>> GetWorkspaceTemplatesAsync(int workspaceId)
        {
            return await _context.MessageTemplates
                .Where(t => t.WorkspaceId == workspaceId)
                .OrderBy(t => t.TemplateType)
                .ThenBy(t => t.Channel)
                .ToListAsync();
        }

        public async Task<MessageTemplate> CreateTemplateAsync(MessageTemplate template)
        {
            // Eğer yeni template aktifse, aynı tip ve kanaldaki diğer template'leri pasif yap
            if (template.IsActive)
            {
                var existingActiveTemplates = await _context.MessageTemplates
                    .Where(t => t.WorkspaceId == template.WorkspaceId 
                             && t.TemplateType == template.TemplateType 
                             && t.Channel == template.Channel
                             && t.IsActive)
                    .ToListAsync();

                foreach (var existingTemplate in existingActiveTemplates)
                {
                    existingTemplate.IsActive = false;
                    existingTemplate.UpdatedAt = DateTime.UtcNow;
                }
            }

            template.CreatedAt = DateTime.UtcNow;
            _context.MessageTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<MessageTemplate> UpdateTemplateAsync(MessageTemplate template)
        {
            // Eğer template aktif ediliyorsa, aynı tip ve kanaldaki diğerleri pasif yap
            if (template.IsActive)
            {
                var otherActiveTemplates = await _context.MessageTemplates
                    .Where(t => t.WorkspaceId == template.WorkspaceId 
                             && t.TemplateType == template.TemplateType 
                             && t.Channel == template.Channel
                             && t.Id != template.Id
                             && t.IsActive)
                    .ToListAsync();

                foreach (var otherTemplate in otherActiveTemplates)
                {
                    otherTemplate.IsActive = false;
                    otherTemplate.UpdatedAt = DateTime.UtcNow;
                }
            }

            template.UpdatedAt = DateTime.UtcNow;
            _context.MessageTemplates.Update(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int templateId, int workspaceId)
        {
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.WorkspaceId == workspaceId && !t.IsDefault);
            
            if (template == null) return false;

            _context.MessageTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ProcessTemplateAsync(string templateBody, Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(templateBody)) return "";

            var result = templateBody;

            // {{variable}} formatındaki değişkenleri değiştir
            var regex = new Regex(@"\{\{([^}]+)\}\}");
            result = regex.Replace(result, match =>
            {
                var key = match.Groups[1].Value.Trim();
                
                // Nested property desteği (örn: customer.name)
                var parts = key.Split('.');
                object value = null;

                if (parts.Length == 1)
                {
                    data.TryGetValue(key, out value);
                }
                else if (parts.Length == 2 && data.TryGetValue(parts[0], out var parentObj))
                {
                    if (parentObj != null)
                    {
                        var prop = parentObj.GetType().GetProperty(parts[1]);
                        if (prop != null)
                        {
                            value = prop.GetValue(parentObj);
                        }
                    }
                }

                return value?.ToString() ?? match.Value;
            });

            // Conditional blocks: {{#if variable}}...{{/if}}
            var conditionalRegex = new Regex(@"\{\{#if\s+([^}]+)\}\}(.*?)\{\{/if\}\}", RegexOptions.Singleline);
            result = conditionalRegex.Replace(result, match =>
            {
                var condition = match.Groups[1].Value.Trim();
                var content = match.Groups[2].Value;

                if (data.TryGetValue(condition, out var value) && 
                    value != null && 
                    !string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return content;
                }
                return "";
            });

            return result;
        }

        public async Task<(string subject, string body)> GetProcessedEmailContentAsync(
            int workspaceId, 
            TemplateType type, 
            Dictionary<string, object> data)
        {
            var template = await GetTemplateAsync(workspaceId, type, TemplateChannel.Email);
            if (template == null) return ("", "");

            var processedSubject = await ProcessTemplateAsync(template.Subject, data);
            var processedBody = await ProcessTemplateAsync(template.Body, data);

            return (processedSubject, processedBody);
        }

        public async Task<string> GetProcessedWhatsAppContentAsync(
            int workspaceId, 
            TemplateType type, 
            Dictionary<string, object> data)
        {
            var template = await GetTemplateAsync(workspaceId, type, TemplateChannel.WhatsApp);
            if (template == null) return "";

            return await ProcessTemplateAsync(template.Body, data);
        }
    }
}