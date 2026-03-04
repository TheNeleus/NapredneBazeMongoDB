using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RpgMongoDb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class ItemService
    {
        private readonly IMongoCollection<GameItem> _itemsCollection;

        public ItemService(IMongoClient client, IConfiguration config)
        {
            var database = client.GetDatabase(config["RpgDatabaseSettings:DatabaseName"]);
            
            _itemsCollection = database.GetCollection<GameItem>("GameItems");
        }

        public async Task CreateItemAsync(GameItem newItem)
        {
            await _itemsCollection.InsertOneAsync(newItem);
        }

        public async Task<List<GameItem>> GetAllItemsAsync()
        {
            return await _itemsCollection.Find(_ => true).ToListAsync();
        }
    }
}