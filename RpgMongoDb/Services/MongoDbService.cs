using MongoDB.Driver;
using RpgMongoDb.Models;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Services
{
    public class MongoDbService
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Player> _playersCollection;
        private readonly IMongoCollection<Auction> _auctionsCollection;

        public MongoDbService(string connectionString, string databaseName)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            
            _playersCollection = _database.GetCollection<Player>("Players");
            _auctionsCollection = _database.GetCollection<Auction>("Auctions");
        }

        public async Task<List<Auction>> GetActiveAuctionsAsync()
        {
            return await _auctionsCollection.Find(a => a.Status == "Active").ToListAsync();
        }

        // --- CREATE: Postavljanje predmeta na aukciju (Transakcija 1) ---
        public async Task CreateAuctionAsync(string playerId, string itemId, decimal startingPrice)
        {
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                var player = await _playersCollection.Find(session, p => p.Id == playerId).FirstOrDefaultAsync();
                if (player == null) throw new Exception("Igrač nije pronađen!");

                var item = player.Inventory.FirstOrDefault(p => p.ItemId == itemId);
                if (item == null) throw new Exception("Predmet nije pronađen u inventaru!");

                var filter = Builders<Player>.Filter.Eq(i => i.Id, playerId);
                var update = Builders<Player>.Update.PullFilter(i => i.Inventory, p => p.ItemId == itemId);
                await _playersCollection.UpdateOneAsync(session, filter, update);

                var auction = new Auction
                {
                    SellerId = playerId,
                    Item = item,
                    StartingPrice = startingPrice,
                    CurrentBid = startingPrice,
                    ExpirationTime = DateTime.UtcNow.AddHours(24),
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
            // Započinjemo sesiju za ACID transakciju
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                // 1. Dohvatanje trenutnog stanja aukcije
                var auction = await _auctionsCollection
                    .Find(session, a => a.Id == auctionId)
                    .FirstOrDefaultAsync();

                if (auction == null || auction.Status != "Active")
                    throw new Exception("Aukcija ne postoji ili je završena.");

                if (bidAmount <= auction.CurrentBid || bidAmount < auction.StartingPrice)
                    throw new Exception("Ponuda mora biti veća od trenutne cene!");

                // 2. Dohvatanje igrača i provera da li ima dovoljno zlata
                var player = await _playersCollection
                    .Find(session, p => p.Id == playerId)
                    .FirstOrDefaultAsync();

                if (player == null || player.Gold < bidAmount)
                    throw new Exception("Nemate dovoljno zlata za ovu ponudu.");

                // 3. Atomsko ažuriranje aukcije (Optimističko zaključavanje)
                // Filter proverava ID aukcije ALI I DA LI JE TRENUTNA CENA OSTALA ISTA!
                var auctionFilter = Builders<Auction>.Filter.And(
                    Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                    Builders<Auction>.Filter.Eq(a => a.CurrentBid, auction.CurrentBid) 
                );

                var auctionUpdate = Builders<Auction>.Update
                    .Set(a => a.CurrentBid, bidAmount)
                    .Set(a => a.HighestBidderId, playerId);

                var updateResult = await _auctionsCollection.UpdateOneAsync(session, auctionFilter, auctionUpdate);

                // Ako je ModifiedCount 0, to znači da je u milisekundi neko drugi već promenio cenu
                if (updateResult.ModifiedCount == 0)
                    throw new Exception("Neko je upravo dao veću ponudu! Pokušajte ponovo.");

                // 4. Oduzimanje zlata novom ponuđaču
                var deductGoldUpdate = Builders<Player>.Update.Inc(p => p.Gold, -bidAmount);
                await _playersCollection.UpdateOneAsync(session, p => p.Id == playerId, deductGoldUpdate);

                // 5. Vraćanje zlata prethodnom ponuđaču (ako postoji)
                if (!string.IsNullOrEmpty(auction.HighestBidderId))
                {
                    var refundGoldUpdate = Builders<Player>.Update.Inc(p => p.Gold, auction.CurrentBid);
                    await _playersCollection.UpdateOneAsync(session, p => p.Id == auction.HighestBidderId, refundGoldUpdate);
                }

                // Sve je prošlo u redu, sačuvaj promene u bazi
                await session.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Ako bilo šta pukne (igrač nema para, baza odbije upit, pukne mreža), poništi SVE
                await session.AbortTransactionAsync();
                throw;
            }
        }
    }
}