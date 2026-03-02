using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RpgMongoDb.Models
{
    public class LootBox
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("box_id")]
        public string BoxId { get; set; } = null!; 

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("target_item_type")]
        public string TargetItemType { get; set; } = null!; 
    }
}