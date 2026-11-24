// Enumerates the applications that originate requests.
using System.Text.Json.Serialization;

namespace Giraf_AI.Models;

/// <summary>
/// Identifies the GIRAF application that originated a request.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OriginApp
{
    fp,
    vta
}