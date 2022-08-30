using Repositories.Interfaces;
using Repositories.Models;
using Restaurant.Kitchen.Models;
using System;
using System.Data.SQLite;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace Repositoties
{
    public class KitchenIdempotencytRepository : IDataBaseRepositoty<TableBookedModel>
    {
        private const string selectFromKitchen = "SELECT MessageId FROM KitchenIdempotency WHERE MessageId = @MessageId LIMIT 1";
        private const string insertToKitchen = "INSERT INTO KitchenIdempotency (MessageId, OrderId) VALUES(@MessageId, @OrderId)";
        private const string deleteFromKitchen = "DELETE FROM KitchenIdempotency WHERE MessageId = @MessageId;";
        private const string selectAllFromKitchen = "SELECT * FROM KitchenIdempotency";

        private readonly ILogger _logger;

        private Timer _timer;

        public KitchenIdempotencytRepository(ILogger<KitchenIdempotencytRepository> logger)
        {
            _logger = logger;
        }

        public async Task Add(TableBookedModel entity)
        {
            _logger.LogInformation($"KitchenIdempotencytRepository request Add entity.OrderId={entity.OrderId}");

            using (var con = new SQLiteConnection(RepositoryConnectionSettings.ConnectionString))
            {
                await con.OpenAsync();

                using var command = new SQLiteCommand(insertToKitchen, con);
                command.Parameters.AddWithValue("@MessageId", entity.MessageId);
                command.Parameters.AddWithValue("@OrderId", entity.OrderId);
                await command.PrepareAsync();

                await command.ExecuteNonQueryAsync();
            }

            _timer = new(30_000);
            _timer.Elapsed += async (sender, e) => await Delete(entity.MessageId);
            _timer.AutoReset = false;
            _timer.Start();

            //await PrintTable("Add");
        }

        public async Task<bool> Contains(string MessageId)
        {
            _logger.LogInformation($"KitchenIdempotencytRepository request Contains MessageId={MessageId}");

            using (var con = new SQLiteConnection(RepositoryConnectionSettings.ConnectionString))
            {
                await con.OpenAsync();

                using var command = new SQLiteCommand(selectFromKitchen, con);
                command.Parameters.AddWithValue("@MessageId", MessageId);
                await command.PrepareAsync();

                var result = await command.ExecuteScalarAsync();
                
                if(result == null)
                {
                    _logger.LogDebug($"KitchenIdempotencytRepository request Contains MessageId={MessageId} result is null!");
                    return false;
                }
            }

            _logger.LogDebug($"KitchenIdempotencytRepository request Contains MessageId={MessageId} finded");
            return true;
        }

        private async Task Delete(string MessageId)
        {
            _logger.LogInformation($"KitchenIdempotencytRepository request Delete MessageId={MessageId}");

            using (var con = new SQLiteConnection(RepositoryConnectionSettings.ConnectionString))
            {
                await con.OpenAsync();

                using var command = new SQLiteCommand(deleteFromKitchen, con);
                command.Parameters.AddWithValue("@MessageId", MessageId);
                await command.PrepareAsync();

                await command.ExecuteNonQueryAsync();
            }

            //await PrintTable("Delete");
        }

        private async Task PrintTable(string operationName)
        {
            using (var con = new SQLiteConnection(RepositoryConnectionSettings.ConnectionString))
            {
                await con.OpenAsync();

                using var command = new SQLiteCommand(selectAllFromKitchen, con);
                System.Data.Common.DbDataReader myReader = await command.ExecuteReaderAsync();
                while (await myReader.ReadAsync())
                {
                    Console.WriteLine(myReader.GetString(0) + " <--orderId " + operationName + " MessId--> " + myReader.GetGuid(1));
                }
            }
        }
    }
}
