using Microsoft.AspNetCore.Mvc;
using RpgMongoDb.Models;
using RpgMongoDb.Services;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LootBoxController : ControllerBase
    {
        private readonly LootBoxService _lootBoxService;

        public LootBoxController(LootBoxService lootBoxService)
        {
            _lootBoxService = lootBoxService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLootBoxes()
        {
            try
            {
                var boxes = await _lootBoxService.GetAllLootBoxesAsync();
                return Ok(boxes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}