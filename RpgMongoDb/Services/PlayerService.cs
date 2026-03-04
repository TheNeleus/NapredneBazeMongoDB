using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RpgMongoDb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class PlayerService
    {
        private readonly IMongoCollection<Player> _playersCollection;
        private readonly IMongoCollection<LootBox> _lootBoxesCollection;
        private readonly IMongoCollection<GameItem> _gameItemsCollection;

        public PlayerService(IMongoClient client, IConfiguration config)
        {
            var database = client.GetDatabase(config["RpgDatabaseSettings:DatabaseName"]);

            _playersCollection = database.GetCollection<Player>("Players");
            _lootBoxesCollection = database.GetCollection<LootBox>("LootBoxes");
            _gameItemsCollection = database.GetCollection<GameItem>("GameItems");
        }

        public async Task CreatePlayerAsync(Player newPlayer)
        {
            var existingPlayer = await _playersCollection
                .Find(p => p.Username == newPlayer.Username)
                .FirstOrDefaultAsync();

            if (existingPlayer != null)
            {
                throw new Exception("To korisničko ime je već zauzeto! Molimo te izaberi neko drugo.");
            }

            await _playersCollection.InsertOneAsync(newPlayer);
        }
        
        public async Task<Player?> GetPlayerAsync(string id)
        {
            return await _playersCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeletePlayerAsync(string id)
        {
            await _playersCollection.DeleteOneAsync(p => p.Id == id);
        }
        
        public async Task<string> OpenLootBoxAsync(string playerId, string boxId)
        {
            var lootBox = await _lootBoxesCollection.Find(b => b.BoxId == boxId).FirstOrDefaultAsync();
            if (lootBox == null) throw new Exception("Loot box ne postoji!");

            var possibleItems = await _gameItemsCollection.Find(i => i.Type == lootBox.TargetItemType).ToListAsync();
            if (!possibleItems.Any()) throw new Exception($"Nema predmeta tipa '{lootBox.TargetItemType}' u katalogu!");

            int totalWeight = possibleItems.Sum(x => x.DropWeight);
            int randomValue = Random.Shared.Next(0, totalWeight);

            GameItem? wonGameItem = null;
            foreach (var item in possibleItems)
            {
                if (randomValue < item.DropWeight)
                {
                    wonGameItem = item;
                    break;
                }
                randomValue -= item.DropWeight;
            }

            if (wonGameItem == null) throw new Exception("Greška pri izvlačenju predmeta!");

            var inventoryItem = new Item
            {
                ItemId = wonGameItem.ItemId,
                Name = wonGameItem.Name,
                Quantity = 1
            };

            var player = await _playersCollection.Find(p => p.Id == playerId).FirstOrDefaultAsync();
            if (player == null) throw new Exception("Igrač nije pronađen.");

            bool hasItem = player.Inventory.Any(i => i.ItemId == wonGameItem.ItemId);

            if (hasItem)
            {
                var filter = Builders<Player>.Filter.And(
                    Builders<Player>.Filter.Eq(p => p.Id, playerId),
                    Builders<Player>.Filter.ElemMatch(p => p.Inventory, i => i.ItemId == wonGameItem.ItemId)
                );
                var update = Builders<Player>.Update.Inc("Inventory.$.Quantity", 1);
                await _playersCollection.UpdateOneAsync(filter, update);
            }
            else
            {
                var filter = Builders<Player>.Filter.Eq(p => p.Id, playerId);
                var update = Builders<Player>.Update.Push(p => p.Inventory, inventoryItem);
                await _playersCollection.UpdateOneAsync(filter, update);
            }

            return $"Otvorili ste '{lootBox.Name}' i dobili: {wonGameItem.Name}!";
        }
        
        public async Task<string> LoginAsync(string username)
        {
            var player = await _playersCollection.Find(p => p.Username == username).FirstOrDefaultAsync();

            if (player == null)
            {
                throw new Exception("Korisnik sa tim imenom ne postoji. Molimo vas da se prvo registrujete.");
            }

            return player.Id!;
        }
    }
}