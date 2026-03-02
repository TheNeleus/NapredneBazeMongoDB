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
        private readonly AuctionService _auctionService;

        public AuctionController(AuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(
            [FromQuery] string playerId, 
            [FromQuery] string itemId, 
            [FromQuery] int quantity, 
            [FromQuery] decimal startingPrice, 
            [FromQuery] int durationInHours)
        {
            try
            {
                await _auctionService.CreateAuctionAsync(playerId, itemId, quantity, startingPrice, durationInHours);
                return Ok(new { message = "Predmet uspešno postavljen na aukciju!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAuctions()
        {
            try
            {
                var auctions = await _auctionService.GetActiveAuctionsAsync();
                return Ok(auctions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("bid")]
        public async Task<IActionResult> PlaceBid([FromQuery] string auctionId, [FromQuery] string playerId, [FromQuery] decimal bidAmount)
        {
            try
            {
                await _auctionService.PlaceBidAsync(auctionId, playerId, bidAmount);
                return Ok(new { message = "Uspešno ste licitirali!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> ResolveAuctions()
        {
            try
            {
                await _auctionService.ResolveExpiredAuctionsAsync();
                return Ok(new { message = "Sve istekle aukcije su uspešno razrešene!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}