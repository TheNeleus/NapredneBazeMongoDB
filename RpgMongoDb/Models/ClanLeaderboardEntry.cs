using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RpgMongoDb.Models
{
    public class ClanLeaderboardEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ClanId { get; set; }

        [BsonElement("clanName")]
        public string ClanName { get; set; } = null!;

        [BsonElement("totalGold")]
        public decimal TotalGold { get; set; }
    }
}