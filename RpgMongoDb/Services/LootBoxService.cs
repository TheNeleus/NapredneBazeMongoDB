using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RpgMongoDb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class LootBoxService
    {
        private readonly IMongoCollection<LootBox> _lootBoxesCollection;

        public LootBoxService(IMongoClient client, IConfiguration config)
        {
            var database = client.GetDatabase(config["RpgDatabaseSettings:DatabaseName"]);
            _lootBoxesCollection = database.GetCollection<LootBox>("LootBoxes");
        }

        public async Task<List<LootBox>> GetAllLootBoxesAsync()
        {
            return await _lootBoxesCollection.Find(_ => true).ToListAsync();
        }

        public async Task CreateLootBoxAsync(LootBox newBox)
        {
            await _lootBoxesCollection.InsertOneAsync(newBox);
        }
    }
}