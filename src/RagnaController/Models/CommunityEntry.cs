using System.Text.Json.Serialization;

namespace RagnaController.Models
{
    public class CommunityEntry
    {
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShareCode { get; set; } = ""; // Can be the 6-char code or the full Gist ID
    }
}
