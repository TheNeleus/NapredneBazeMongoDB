using Microsoft.AspNetCore.Mvc;
using RpgMongoDb.Models;
using RpgMongoDb.Services;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        // Dependency Injection servisa za bazu
        public AuctionController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // POST: api/auction/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(
            [FromQuery] string playerId, 
            [FromQuery] string itemId, 
            [FromQuery] decimal startingPrice)
        {
            try
            {
                await _mongoDbService.CreateAuctionAsync(playerId, itemId, startingPrice);
                return Ok(new { message = "Item successfully placed on auction!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/auction/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAuctions()
        {
            try
            {
                var auctions = await _mongoDbService.GetActiveAuctionsAsync();
                return Ok(auctions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/auction/bid
        // NOVI ENDPOINT za bidovanje
        [HttpPost("bid")]
        public async Task<IActionResult> PlaceBid(
            [FromQuery] string auctionId, 
            [FromQuery] string playerId, 
            [FromQuery] decimal bidAmount)
        {
            try
            {
                await _mongoDbService.PlaceBidAsync(auctionId, playerId, bidAmount);
                return Ok(new { message = "Bid successfully placed!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}