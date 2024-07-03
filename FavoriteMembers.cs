using System.Text.Json.Serialization;

namespace SyncRooms
{
    internal class FavoriteMembers
    {
        public partial class FavRoot
        {
            [JsonPropertyName("favs")]
            public List<Fav> Members { get; set; } = [];

            public void Add(Fav item)
            {
                Members ??= [];
                if (item is null) { return; }
                Members.Add(item);
            }

            public void Remove(Fav item)
            {
                if (item is null) { return; }
                Members.Remove(item);
            }
        }

        public partial class Fav
        {
            [JsonPropertyName("userId")]
            public string UserId { get; set; } = string.Empty;

            [JsonPropertyName("nickname")]
            public string Nickname { get; set; } = string.Empty;

            [JsonPropertyName("alertOn")]
            public bool AlertOn { get; set; } = false;
        }

        public FavoriteMembers()
        {

        }
    }
}
