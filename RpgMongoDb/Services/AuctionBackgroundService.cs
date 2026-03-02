using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RpgMongoDb.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RpgMongoDb.BackgroundServices
{
    public class AuctionBackgroundService : BackgroundService
    {
        private readonly AuctionService _auctionService;
        private readonly ILogger<AuctionBackgroundService> _logger;

        public AuctionBackgroundService(AuctionService auctionService, ILogger<AuctionBackgroundService> logger)
        {
            _auctionService = auctionService;
            _logger = logger;
        }

         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Aukcijski pozadinski radnik je POKRENUT.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Proveravam i završavam istekle aukcije...");
                    
                    await _auctionService.ResolveExpiredAuctionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Desila se greška prilikom obrade aukcija u pozadini.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}