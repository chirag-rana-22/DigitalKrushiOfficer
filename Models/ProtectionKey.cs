using System.ComponentModel.DataAnnotations;
namespace Digital_Krushi_Officer.Models;

public sealed class ProtectionKey
{
    public int Id { get; set; }
    [MaxLength(200)] public string FriendlyName { get; set; } = "";
    public string Xml { get; set; } = "";
}
