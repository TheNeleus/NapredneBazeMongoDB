using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RpgMongoDb.Models
{
    public class GameItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("item_id")]
        public string ItemId { get; set; } = null!; 

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("type")]
        public string Type { get; set; } = null!; // "Weapon", "Potion", "Armor"

        [BsonElement("drop_weight")]
        public int DropWeight { get; set; } 
    }
}