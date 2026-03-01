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

        public ClanService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            
            _clansCollection = database.GetCollection<Clan>("Clans");
            // Trebaju nam i igrači zbog agregacije bogatstva i učlanjenja
            _playersCollection = database.GetCollection<Player>("Players");
        }

        // --- CREATE: Pravljenje novog klana ---
        public async Task CreateClanAsync(Clan clan)
        {
            await _clansCollection.InsertOneAsync(clan);
        }

        // --- UPDATE: Dodavanje igrača u klan ---
        public async Task JoinClanAsync(string playerId, string clanId)
        {
            var update = Builders<Player>.Update.Set(p => p.ClanId, clanId);
            await _playersCollection.UpdateOneAsync(p => p.Id == playerId, update);
        }

        // --- AGGREGATION: Top 5 Klanova (Liderbord) ---
        public async Task<List<ClanLeaderboardItem>> GetTopClansByWealthAsync()
        {
            var pipeline = new BsonDocument[]
            {
                // 1. Ignoriši igrače koji nemaju klan
                new BsonDocument("$match", new BsonDocument("clan_id", new BsonDocument("$ne", BsonNull.Value))),
                
                // 2. Grupiši po clan_id i saberi zlato i broj članova
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$clan_id" },
                    { "TotalClanGold", new BsonDocument("$sum", "$gold") },
                    { "MemberCount", new BsonDocument("$sum", 1) }
                }),
                
                // 3. Spoji sa kolekcijom "Clans" da dobiješ pravo ime klana
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Clans" },
                    { "localField", "_id" },
                    { "foreignField", "_id" },
                    { "as", "ClanDetails" }
                }),
                
                // 4. Raspakuj niz koji je napravio lookup
                new BsonDocument("$unwind", "$ClanDetails"),
                
                // 5. Sortiraj po ukupnom zlatu opadajuće
                new BsonDocument("$sort", new BsonDocument("TotalClanGold", -1)),
                
                // 6. Ograniči na top 5
                new BsonDocument("$limit", 5),
                
                // 7. Formatiraj izlaz
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "ClanName", "$ClanDetails.name" },
                    { "TotalGold", "$TotalClanGold" },
                    { "Members", "$MemberCount" }
                })
            };

            // Izvrši agregaciju nad Players kolekcijom
            var cursor = await _playersCollection.AggregateAsync<BsonDocument>(pipeline);
            var rawResults = await cursor.ToListAsync();

            // Mapiraj MongoDB BsonDocument u čiste C# objekte za lakše slanje
            var leaderboard = new List<ClanLeaderboardItem>();
            foreach (var doc in rawResults)
            {
                leaderboard.Add(new ClanLeaderboardItem
                {
                    ClanName = doc["ClanName"].AsString,
                    // Castovanje tipova u zavisnosti kako su sačuvani u bazi
                    TotalGold = doc["TotalGold"].ToDecimal(),
                    Members = doc["Members"].ToInt32()
                });
            }

            return leaderboard;
        }
    }
}