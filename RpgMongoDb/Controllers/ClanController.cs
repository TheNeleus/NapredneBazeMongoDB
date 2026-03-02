using Microsoft.AspNetCore.Mvc;
using RpgMongoDb.Models;
using RpgMongoDb.Services;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClanController : ControllerBase
    {
        private readonly ClanService _clanService;

        public ClanController(ClanService clanService)
        {
            _clanService = clanService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateClan([FromBody] Clan newClan)
        {
            try
            {
                await _clanService.CreateClanAsync(newClan);
                return Ok(new { message = "Clan successfully created!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinClan([FromQuery] string playerId, [FromQuery] string clanId)
        {
            try
            {
                await _clanService.JoinClanAsync(playerId, clanId);
                return Ok(new { message = "Player successfully joined the clan!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            try
            {
                var leaderboard = await _clanService.GetTopClansByWealthAsync();
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllClans()
        {
            try
            {
                var clans = await _clanService.GetAllClansAsync();
                return Ok(clans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}