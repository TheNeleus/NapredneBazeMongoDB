using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RpgMongoDb.Models
{
    public class Auction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("seller_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SellerId { get; set; } = null!;

        [BsonElement("item")]
        public Item Item { get; set; } = null!;

        [BsonElement("starting_price")]
        public decimal StartingPrice { get; set; }

        [BsonElement("current_bid")]
        public decimal CurrentBid { get; set; }

        [BsonElement("highest_bidder_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? HighestBidderId { get; set; }

        [BsonElement("expiration_time")]
        public DateTime ExpirationTime { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = null!;
    }
}