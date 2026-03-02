using MongoDB.Driver;
using RpgMongoDb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class LootBoxService
    {
        private readonly IMongoCollection<LootBox> _lootBoxesCollection;

        public LootBoxService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _lootBoxesCollection = database.GetCollection<LootBox>("LootBoxes");
        }

        public async Task<List<LootBox>> GetAllLootBoxesAsync()
        {
            return await _lootBoxesCollection.Find(_ => true).ToListAsync();
        }
    }
}