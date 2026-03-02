using Microsoft.AspNetCore.Mvc;
using RpgMongoDb.Models;
using RpgMongoDb.Services;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerService _playerService;

        public PlayerController(PlayerService playerService)
        {
            _playerService = playerService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePlayer([FromBody] Player newPlayer)
        {
            try
            {
                await _playerService.CreatePlayerAsync(newPlayer);
                return Ok(new { message = "Player successfully created!", playerId = newPlayer.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(string id)
        {
            try
            {
                var player = await _playerService.GetPlayerAsync(id);
                if (player == null) return NotFound(new { message = "Player not found." });

                return Ok(player);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(string id)
        {
            try
            {
                await _playerService.DeletePlayerAsync(id);
                return Ok(new { message = "Player successfully deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/lootbox/{boxId}")]
        public async Task<IActionResult> OpenLootBox(string id, string boxId)
        {
            try
            {
                var resultMessage = await _playerService.OpenLootBoxAsync(id, boxId);
                return Ok(new { message = resultMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromQuery] string username)
        {
            try
            {
                var sessionId = await _playerService.LoginAsync(username);
                return Ok(new { 
                    message = "Uspešan login!", 
                    sessionId = sessionId 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}