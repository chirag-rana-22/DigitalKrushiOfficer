using System.Xml.Linq;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Services;

// Data Protection encrypts these XML records with the deployment certificate before storage.
public sealed class DatabaseKeyRepository(IServiceScopeFactory scopes) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<ProtectionKey>()
            .AsNoTracking().Select(k => k.Xml).ToList().Select(XElement.Parse).ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<ProtectionKey>().Add(new ProtectionKey { FriendlyName = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
        db.SaveChanges();
    }
}
