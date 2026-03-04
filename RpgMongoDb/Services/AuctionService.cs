using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RpgMongoDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class AuctionService 
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Player> _playersCollection;
        private readonly IMongoCollection<Auction> _auctionsCollection;

        public AuctionService(IMongoClient client, IConfiguration config) 
        {
            _client = client;
            _database = _client.GetDatabase(config["RpgDatabaseSettings:DatabaseName"]);
            
            _playersCollection = _database.GetCollection<Player>("Players");
            _auctionsCollection = _database.GetCollection<Auction>("Auctions");
        }

        public async Task<List<Auction>> GetActiveAuctionsAsync()
        {
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Status, "Active"),
                Builders<Auction>.Filter.Gt(a => a.ExpirationTime, DateTime.UtcNow)
            );
            return await _auctionsCollection.Find(filter).ToListAsync();
        }

        public async Task CreateAuctionAsync(string playerId, string itemId, int quantityToSell, decimal startingPrice, int durationInHours)
        {
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                var player = await _playersCollection.Find(session, p => p.Id == playerId).FirstOrDefaultAsync();
                if (player == null) throw new Exception("Igrač nije pronađen!");

                var itemInInventory = player.Inventory.FirstOrDefault(p => p.ItemId == itemId);
                if (itemInInventory == null) throw new Exception("Predmet nije pronađen u inventaru!");

                if (itemInInventory.Quantity < quantityToSell)
                    throw new Exception($"Nemate dovoljno ovog predmeta! Imate samo {itemInInventory.Quantity} komada.");

                if (itemInInventory.Quantity == quantityToSell)
                {
                    var filter = Builders<Player>.Filter.Eq(i => i.Id, playerId);
                    var update = Builders<Player>.Update.PullFilter(i => i.Inventory, p => p.ItemId == itemId);
                    await _playersCollection.UpdateOneAsync(session, filter, update);
                }
                else
                {
                    var filter = Builders<Player>.Filter.And(
                        Builders<Player>.Filter.Eq(p => p.Id, playerId),
                        Builders<Player>.Filter.ElemMatch(p => p.Inventory, i => i.ItemId == itemId)
                    );
                    var update = Builders<Player>.Update.Inc("Inventory.$.Quantity", -quantityToSell);
                    await _playersCollection.UpdateOneAsync(session, filter, update);
                }

                var auctionItem = new Item
                {
                    ItemId = itemInInventory.ItemId,
                    Name = itemInInventory.Name,
                    Quantity = quantityToSell
                };

                var auction = new Auction
                {
                    SellerId = playerId,
                    Item = auctionItem,
                    StartingPrice = startingPrice,
                    CurrentBid = startingPrice,
                    ExpirationTime = DateTime.UtcNow.AddHours(durationInHours),
                    Status = "Active"
                };
                await _auctionsCollection.InsertOneAsync(session, auction);

                await session.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }

        public async Task<bool> PlaceBidAsync(string auctionId, string playerId, decimal bidAmount)
        {
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                var auction = await _auctionsCollection.Find(session, a => a.Id == auctionId).FirstOrDefaultAsync();

                if (auction == null || auction.Status != "Active" || auction.ExpirationTime <= DateTime.UtcNow)
                    throw new Exception("Aukcija ne postoji, završena je ili je istekla.");

                if (bidAmount <= auction.CurrentBid || bidAmount < auction.StartingPrice)
                    throw new Exception("Ponuda mora biti veća od trenutne cene!");

                var player = await _playersCollection.Find(session, p => p.Id == playerId).FirstOrDefaultAsync();

                if (player == null || player.Gold < bidAmount)
                    throw new Exception("Nemate dovoljno zlata za ovu ponudu.");

                var auctionFilter = Builders<Auction>.Filter.And(
                    Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                    Builders<Auction>.Filter.Eq(a => a.CurrentBid, auction.CurrentBid) 
                );

                var auctionUpdate = Builders<Auction>.Update
                    .Set(a => a.CurrentBid, bidAmount)
                    .Set(a => a.HighestBidderId, playerId);

                var updateResult = await _auctionsCollection.UpdateOneAsync(session, auctionFilter, auctionUpdate);

                if (updateResult.ModifiedCount == 0)
                    throw new Exception("Neko je upravo dao veću ponudu! Pokušajte ponovo.");

                var deductGoldUpdate = Builders<Player>.Update.Inc(p => p.Gold, -bidAmount);
                await _playersCollection.UpdateOneAsync(session, p => p.Id == playerId, deductGoldUpdate);

                if (!string.IsNullOrEmpty(auction.HighestBidderId))
                {
                    var refundGoldUpdate = Builders<Player>.Update.Inc(p => p.Gold, auction.CurrentBid);
                    await _playersCollection.UpdateOneAsync(session, p => p.Id == auction.HighestBidderId, refundGoldUpdate);
                }

                await session.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }

        public async Task ResolveExpiredAuctionsAsync()
        {
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Status, "Active"),
                Builders<Auction>.Filter.Lte(a => a.ExpirationTime, DateTime.UtcNow)
            );

            var expiredAuctions = await _auctionsCollection.Find(filter).ToListAsync();

            foreach (var auction in expiredAuctions)
            {
                using var session = await _client.StartSessionAsync();
                session.StartTransaction();

                try
                {
                    if (!string.IsNullOrEmpty(auction.HighestBidderId))
                    {
                        var sellerUpdate = Builders<Player>.Update.Inc(p => p.Gold, auction.CurrentBid);
                        await _playersCollection.UpdateOneAsync(session, p => p.Id == auction.SellerId, sellerUpdate);

                        var buyer = await _playersCollection.Find(session, p => p.Id == auction.HighestBidderId).FirstOrDefaultAsync();
                        if (buyer != null)
                        {
                            bool buyerHasItem = buyer.Inventory.Any(i => i.ItemId == auction.Item.ItemId);
                            if (buyerHasItem)
                            {
                                var buyerFilter = Builders<Player>.Filter.And(
                                    Builders<Player>.Filter.Eq(p => p.Id, auction.HighestBidderId),
                                    Builders<Player>.Filter.ElemMatch(p => p.Inventory, i => i.ItemId == auction.Item.ItemId)
                                );
                                var buyerItemUpdate = Builders<Player>.Update.Inc("Inventory.$.Quantity", auction.Item.Quantity);
                                await _playersCollection.UpdateOneAsync(session, buyerFilter, buyerItemUpdate);
                            }
                            else
                            {
                                var buyerItemUpdate = Builders<Player>.Update.Push(p => p.Inventory, auction.Item);
                                await _playersCollection.UpdateOneAsync(session, p => p.Id == auction.HighestBidderId, buyerItemUpdate);
                            }
                        }

                        var auctionUpdate = Builders<Auction>.Update.Set(a => a.Status, "Sold");
                        await _auctionsCollection.UpdateOneAsync(session, a => a.Id == auction.Id, auctionUpdate);
                    }
                    else
                    {
                        var seller = await _playersCollection.Find(session, p => p.Id == auction.SellerId).FirstOrDefaultAsync();
                        if (seller != null)
                        {
                            bool sellerHasItem = seller.Inventory.Any(i => i.ItemId == auction.Item.ItemId);
                            if (sellerHasItem)
                            {
                                var sellerFilter = Builders<Player>.Filter.And(
                                    Builders<Player>.Filter.Eq(p => p.Id, auction.SellerId),
                                    Builders<Player>.Filter.ElemMatch(p => p.Inventory, i => i.ItemId == auction.Item.ItemId)
                                );
                                var sellerItemUpdate = Builders<Player>.Update.Inc("Inventory.$.Quantity", auction.Item.Quantity);
                                await _playersCollection.UpdateOneAsync(session, sellerFilter, sellerItemUpdate);
                            }
                            else
                            {
                                var sellerItemUpdate = Builders<Player>.Update.Push(p => p.Inventory, auction.Item);
                                await _playersCollection.UpdateOneAsync(session, p => p.Id == auction.SellerId, sellerItemUpdate);
                            }
                        }

                        var auctionUpdate = Builders<Auction>.Update.Set(a => a.Status, "Expired");
                        await _auctionsCollection.UpdateOneAsync(session, a => a.Id == auction.Id, auctionUpdate);
                    }

                    await session.CommitTransactionAsync();
                }
                catch (Exception)
                {
                    await session.AbortTransactionAsync();
                }
            }
        }
    }
}