using System.Text.Json;
using Digital_Krushi_Officer.Models;
namespace Digital_Krushi_Officer.Services;
public static class QueryMedia {
    public static IReadOnlyList<string> Paths(TblQuery query) {
        if (!string.IsNullOrWhiteSpace(query.ImagePathsJson)) {
            try { var paths=JsonSerializer.Deserialize<List<string>>(query.ImagePathsJson); if(paths?.Count>0) return paths; }
            catch(JsonException) { }
        }
        return query.ImagePath == null ? Array.Empty<string>() : new[] { query.ImagePath };
    }
}
