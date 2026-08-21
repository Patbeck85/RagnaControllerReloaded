using System.Collections.Generic;
using System.Text.Json.Serialization;
using RagnaController.Profiles;
using RagnaController.Models;

namespace RagnaController
{
    [JsonSerializable(typeof(Profile))]
    [JsonSerializable(typeof(List<Profile>))]
    [JsonSerializable(typeof(Models.Settings))]
    [JsonSerializable(typeof(ButtonAction))]
    [JsonSerializable(typeof(VirtualKey))]
    [JsonSerializable(typeof(Models.ButtonKey))]
    [JsonSerializable(typeof(CommunityEntry))]
    [JsonSerializable(typeof(List<CommunityEntry>))]
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        UseStringEnumConverter = true,
        WriteIndented = true)]
    internal partial class AppJsonContext : JsonSerializerContext { }
}