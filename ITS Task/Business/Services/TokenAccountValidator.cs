using System.Security.Claims;
using ITS_Task.DataAccess;
using ITS_Task.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.Business.Services;
public static class TokenAccountValidator
{
    public static async Task ValidateAsync(TokenValidatedContext validation)
    {
        var id = validation.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) { validation.Fail("Invalid identity"); return; }
        var services = validation.HttpContext.RequestServices;
        var context = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(id);
        if (user == null || await context.Employees.AnyAsync(e => e.UserId == id && !e.IsActive))
        { validation.Fail("Account is unavailable"); return; }
        var current = (await users.GetRolesAsync(user)).OrderBy(r => r).ToArray();
        var supplied = validation.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).OrderBy(r => r).ToArray();
        if (!current.SequenceEqual(supplied)) validation.Fail("Roles changed; sign in again");
    }
}
