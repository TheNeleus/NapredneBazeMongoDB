using Microsoft.AspNetCore.Mvc;
using RpgMongoDb.Models;
using RpgMongoDb.Services;
using System;
using System.Threading.Tasks;

namespace RpgMongoDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly ItemService _itemService;

        public ItemController(ItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateItem([FromBody] GameItem newItem)
        {
            try
            {
                await _itemService.CreateItemAsync(newItem);
                return Ok(new { message = $"Item '{newItem.Name}' successfully added to global catalog!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllItems()
        {
            try
            {
                var items = await _itemService.GetAllItemsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}