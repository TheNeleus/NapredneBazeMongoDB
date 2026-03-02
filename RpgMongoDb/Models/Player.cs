using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace RpgMongoDb.Models
{
    public class Player
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("gold")]
        public decimal Gold { get; set; }

        [BsonElement("clan_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ClanId { get; set; } 

        [BsonElement("inventory")]
        public List<Item> Inventory { get; set; } = new List<Item>();
    }
}