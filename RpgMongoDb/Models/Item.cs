using MongoDB.Bson.Serialization.Attributes;

namespace RpgMongoDb.Models
{
    public class Item
    {
        [BsonElement("item_id")]
        public string ItemId { get; set; } = null!;

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("quantity")]
        public int Quantity { get; set; }
    }
}