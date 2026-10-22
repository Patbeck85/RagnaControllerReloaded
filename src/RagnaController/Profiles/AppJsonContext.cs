using System.Collections.Generic;
using System.Text.Json.Serialization;
using RagnaController.Profiles;
using RagnaController.Models;

namespace RagnaController
{
    [JsonSerializable(typeof(Profile))]
    [JsonSerializable(typeof(List<Profile>))]
    [JsonSerializable(typeof(ProfileTemplate))]
    [JsonSerializable(typeof(TemplateEngineSettings))]
    [JsonSerializable(typeof(TemplateEngineModeSettings))]
    [JsonSerializable(typeof(TemplateSensitivity))]
    [JsonSerializable(typeof(Models.Settings))]
    [JsonSerializable(typeof(ButtonAction))]
    [JsonSerializable(typeof(VirtualKey))]
    [JsonSerializable(typeof(Models.ButtonKey))]
    [JsonSerializable(typeof(CommunityEntry))]
    [JsonSerializable(typeof(List<CommunityEntry>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(ControllerConfig))]
    [JsonSerializable(typeof(Dictionary<string, ControllerConfig>))]
    // TASK-2301: Profile Inheritance
    [JsonSerializable(typeof(BaseProfile))]
    [JsonSerializable(typeof(ClassProfile))]
    [JsonSerializable(typeof(CharProfile))]
    [JsonSerializable(typeof(List<CharProfile>))]
    // FEAT-011: ItemManager — verwaltete Items (Potions)
    [JsonSerializable(typeof(Models.ItemConfig))]
    [JsonSerializable(typeof(List<Models.ItemConfig>))]
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        UseStringEnumConverter = true,
        WriteIndented = true)]
    internal partial class AppJsonContext : JsonSerializerContext { }
}