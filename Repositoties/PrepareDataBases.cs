using Repositories.Models;
using System;
using System.Data.SQLite;

namespace Repositories
{
    public static class PrepareDataBases
    {
        private static string dropTableKitchen = "DROP TABLE IF EXISTS KitchenIdempotency";
        private static string createTableKitchen = @"CREATE TABLE KitchenIdempotency (
                                                        MessageId   CHAR(36) PRIMARY KEY NOT NULL,
                                                        OrderId     CHAR(36))";

        public static void CreateNewKitchenTable()
        {
            Console.WriteLine("PrepareDataBases/CreateNewKitchenTable: Data base connection is " + RepositoryConnectionSettings.ConnectionString);

            using (var con = new SQLiteConnection(RepositoryConnectionSettings.ConnectionString))
            {
                con.Open();

                // удаляем таблицу если она уже существует в базе данных
                using var commandDrop = new SQLiteCommand(dropTableKitchen, con);
                commandDrop.ExecuteNonQuery();

                //создаем таблицу
                using var commandCreate = new SQLiteCommand(createTableKitchen, con);
                commandCreate.ExecuteNonQuery();
            }
        }
    }
}
