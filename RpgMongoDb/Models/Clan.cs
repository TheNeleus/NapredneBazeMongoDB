using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RpgMongoDb.Models
{
    public class Clan
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("description")]
        public string Description { get; set; } = null!;
    }

    public class ClanLeaderboardItem
    {
        public string ClanName { get; set; } = null!;
        public decimal TotalGold { get; set; }
        public int Members { get; set; }
    }
}