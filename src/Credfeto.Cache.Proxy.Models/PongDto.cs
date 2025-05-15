using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Cache.Proxy.Models;

[DebuggerDisplay(value: "{Value}")]
public readonly record struct PongDto
{
    [JsonConstructor]
    public PongDto(string value)
    {
        this.Value = value;
    }

    public string Value { get; }
}
