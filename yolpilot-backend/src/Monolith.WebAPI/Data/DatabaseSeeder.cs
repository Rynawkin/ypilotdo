using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Applications.Commands.Workspace;

namespace Monolith.WebAPI.Data;

public static class DatabaseSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        string[] roleNames = { "SuperAdmin", "Admin", "Dispatcher", "Driver" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };
                await roleManager.CreateAsync(role);
                Console.WriteLine($"Role created: {roleName}");
            }
        }
    }

    public static async Task SeedDemoUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Önce mevcut workspace'leri temizle (fresh start için)
            var existingWorkspaces = await context.Workspaces
                .Where(w => w.Name == "Demo Company" || w.Name == "RotaApp SaaS")
                .ToListAsync();
            
            if (existingWorkspaces.Any())
            {
                Console.WriteLine("Demo workspaces already exist, skipping seed...");
                return;
            }

            // 1. Demo Company Workspace
            var demoWorkspaceCommand = new CreateWorkspaceCommand
            {
                WorkspaceName = "Demo Company",
                AdminUserEmail = "admin@rotaapp.com",
                AdminUserFullName = "Firma Yöneticisi"
            };
            
            var demoWorkspace = new Workspace.Workspace(demoWorkspaceCommand);
            context.Workspaces.Add(demoWorkspace);
            await context.SaveChangesAsync();
            Console.WriteLine($"Workspace created: Demo Company (ID: {demoWorkspace.Id})");

            // 2. Super Admin Workspace
            var superWorkspaceCommand = new CreateWorkspaceCommand
            {
                WorkspaceName = "RotaApp SaaS",
                AdminUserEmail = "super@rotaapp.com",
                AdminUserFullName = "SaaS Admin"
            };
            
            var superWorkspace = new Workspace.Workspace(superWorkspaceCommand);
            context.Workspaces.Add(superWorkspace);
            await context.SaveChangesAsync();
            Console.WriteLine($"Workspace created: RotaApp SaaS (ID: {superWorkspace.Id})");

            // 3. Demo Kullanıcıları Oluştur
            var demoUsers = new[]
            {
                new { 
                    Email = "super@rotaapp.com", 
                    Password = "super123", 
                    FullName = "SaaS Admin",
                    Role = "SuperAdmin",
                    WorkspaceId = superWorkspace.Id,
                    IsAdmin = true,
                    IsDispatcher = false,
                    IsDriver = false,
                    IsSuperAdmin = true // SUPER ADMIN FIELD'I EKLENDİ
                },
                new { 
                    Email = "admin@rotaapp.com", 
                    Password = "admin123", 
                    FullName = "Firma Yöneticisi",
                    Role = "Admin",
                    WorkspaceId = demoWorkspace.Id,
                    IsAdmin = true,
                    IsDispatcher = false,
                    IsDriver = false,
                    IsSuperAdmin = false // Normal admin
                },
                new { 
                    Email = "manager@rotaapp.com", 
                    Password = "manager123", 
                    FullName = "Operasyon Müdürü",
                    Role = "Dispatcher",
                    WorkspaceId = demoWorkspace.Id,
                    IsAdmin = false,
                    IsDispatcher = true,
                    IsDriver = false,
                    IsSuperAdmin = false
                },
                new { 
                    Email = "driver@rotaapp.com", 
                    Password = "driver123", 
                    FullName = "Ali Yılmaz",
                    Role = "Driver",
                    WorkspaceId = demoWorkspace.Id,
                    IsAdmin = false,
                    IsDispatcher = false,
                    IsDriver = true,
                    IsSuperAdmin = false
                }
            };

            foreach (var userData in demoUsers)
            {
                // Kullanıcı zaten var mı kontrol et
                var existingUser = await userManager.FindByEmailAsync(userData.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"User already exists: {userData.Email}");
                    continue;
                }

                // Factory method ile kullanıcı oluştur - IsSuperAdmin parametresi eklendi
                var user = ApplicationUser.CreateForSeed(
                    email: userData.Email,
                    fullName: userData.FullName,
                    workspaceId: userData.WorkspaceId,
                    isAdmin: userData.IsAdmin,
                    isDispatcher: userData.IsDispatcher,
                    isDriver: userData.IsDriver,
                    isSuperAdmin: userData.IsSuperAdmin // YENİ PARAMETRE
                );

                // Password hash'le ve kullanıcıyı oluştur
                var result = await userManager.CreateAsync(user, userData.Password);
                
                if (result.Succeeded)
                {
                    // Role ata
                    await userManager.AddToRoleAsync(user, userData.Role);
                    Console.WriteLine($"✓ Demo user created: {userData.Email} (Role: {userData.Role}, WorkspaceId: {userData.WorkspaceId}, IsSuperAdmin: {userData.IsSuperAdmin})");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create user {userData.Email}:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Description}");
                    }
                }
            }

            Console.WriteLine("\n=== Demo Data Seeding Completed ===");
            Console.WriteLine("Demo Accounts:");
            Console.WriteLine("1. Super Admin: super@rotaapp.com / super123");
            Console.WriteLine("2. Firma Admin: admin@rotaapp.com / admin123");
            Console.WriteLine("3. Manager: manager@rotaapp.com / manager123");
            Console.WriteLine("4. Driver: driver@rotaapp.com / driver123");
            Console.WriteLine("=====================================\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during seeding: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    // Database'i tamamen sıfırlamak için yardımcı metod
    public static async Task ResetDemoDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Console.WriteLine("Resetting demo data...");

        // Demo kullanıcıları sil
        var demoEmails = new[] { "super@rotaapp.com", "admin@rotaapp.com", "manager@rotaapp.com", "driver@rotaapp.com" };
        foreach (var email in demoEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await userManager.DeleteAsync(user);
                Console.WriteLine($"Deleted user: {email}");
            }
        }

        // Demo workspace'leri sil
        var demoWorkspaces = await context.Workspaces
            .Where(w => w.Name == "Demo Company" || w.Name == "RotaApp SaaS")
            .ToListAsync();
        
        context.Workspaces.RemoveRange(demoWorkspaces);
        await context.SaveChangesAsync();
        Console.WriteLine($"Deleted {demoWorkspaces.Count} demo workspaces");

        // Yeniden seed et
        await SeedRolesAsync(serviceProvider);
        await SeedDemoUsersAsync(serviceProvider);
    }
}