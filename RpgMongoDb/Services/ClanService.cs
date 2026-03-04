using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using RpgMongoDb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class ClanService
    {
        private readonly IMongoCollection<Clan> _clansCollection;
        private readonly IMongoCollection<Player> _playersCollection;

        public ClanService(IMongoClient client, IConfiguration config)
        {
            var database = client.GetDatabase(config["RpgDatabaseSettings:DatabaseName"]);

            _clansCollection = database.GetCollection<Clan>("Clans");
            _playersCollection = database.GetCollection<Player>("Players");
        }

        public async Task CreateClanAsync(Clan clan)
        {
            await _clansCollection.InsertOneAsync(clan);
        }

        public async Task JoinClanAsync(string playerId, string clanId)
        {
            var update = Builders<Player>.Update.Set(p => p.ClanId, clanId);
            await _playersCollection.UpdateOneAsync(p => p.Id == playerId, update);
        }

        public async Task<List<ClanLeaderboardItem>> GetTopClansByWealthAsync()
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument("clan_id", new BsonDocument("$ne", BsonNull.Value))),

                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$clan_id" },
                    { "TotalClanGold", new BsonDocument("$sum", "$gold") },
                    { "MemberCount", new BsonDocument("$sum", 1) }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Clans" },
                    { "localField", "_id" },
                    { "foreignField", "_id" },
                    { "as", "ClanDetails" }
                }),
                
                new BsonDocument("$unwind", "$ClanDetails"),

                new BsonDocument("$sort", new BsonDocument("TotalClanGold", -1)),

                new BsonDocument("$limit", 5),

                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "ClanName", "$ClanDetails.name" },
                    { "TotalGold", "$TotalClanGold" },
                    { "Members", "$MemberCount" }
                })
            };

            var cursor = await _playersCollection.AggregateAsync<BsonDocument>(pipeline);
            var rawResults = await cursor.ToListAsync();

            var leaderboard = new List<ClanLeaderboardItem>();
            foreach (var doc in rawResults)
            {
                leaderboard.Add(new ClanLeaderboardItem
                {
                    ClanName = doc["ClanName"].AsString,
                    TotalGold = (decimal)doc["TotalGold"].ToDouble(),
                    Members = doc["Members"].ToInt32()
                });
            }

            return leaderboard;
        }
        
        public async Task<List<Clan>> GetAllClansAsync()
        {
            return await _clansCollection.Find(_ => true).ToListAsync();
        }
    }
}