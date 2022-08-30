﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Lesson1;
using Microsoft.Extensions.Logging;

namespace Restaurant.Booking
{
    public class Restaurant
    {
        private readonly List<Table> _tables = new ();
        private System.Timers.Timer _timerResetTablesBooking;
        private Mutex _mutex = new Mutex();
        private readonly ILogger _logger;

        public Restaurant(ILogger<Restaurant> logger)
        {
            for (ushort i = 1; i <= 10; i++)
            {
                _tables.Add(new Table(i));
            }

            _timerResetTablesBooking = new System.Timers.Timer(300_000);
            _timerResetTablesBooking.AutoReset = true;
            _timerResetTablesBooking.Elapsed += new ElapsedEventHandler(ResetAllTablesBooking);
            _timerResetTablesBooking.Start();

            _logger = logger;
        }

        /// <summary>
        /// Забронировать столик. 
        /// </summary>
        /// <param name="countOfPersons">Количество человек, которое стол должен вместить</param>
        /// <param name="orderId">Номер заказа</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Вернем null или bool</returns>
        public async Task<bool?> BookFreeTableAsync(int countOfPersons, Guid orderId, CancellationToken token = default)
        {
            Console.WriteLine($"Спасибо за Ваше обращение, я подберу столик и подтвержу вашу бронь #{orderId}," +
                              "\r\nВам придет уведомление");

            _mutex.WaitOne();
            Table table = _tables.FirstOrDefault(t => t.SeatsCount > countOfPersons
                                                        && t.State == EnumState.Free);
            var result = table?.SetState(EnumState.Booked, orderId);
            _mutex.ReleaseMutex();

            await Task.Delay(1000 * 1);

            if (result == true)
            {
                _logger.LogDebug($"Restaurant BookFreeTableAsync table {table.Id} booked");
                return true;
            }
            else
            {
                _logger.LogDebug($"Restaurant BookFreeTableAsync table booked failed, null returned");
                return null;
            }
        }

        /// <summary>
        /// Отмена бронирования в результате события консьюмера
        /// </summary>
        /// <param name="orderId">Номер заказа</param>
        /// <param name="token">Токен отмены</param>
        /// <returns></returns>
        public async Task CancelReservationAsync(Guid orderId, CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                var table = _tables.Where(t => t.OrderId == orderId).FirstOrDefault();

                await Task.Delay(1000 * 1, token).ConfigureAwait(true);

                table?.SetState(EnumState.Free);
            }, token);
        }

        /// <summary>
        /// Отмена бронирования клиентом
        /// </summary>
        /// <param name="tableNumber"></param>
        /// <returns></returns>
        internal async Task DeleteBookingForTableAsync(int tableNumber)
        {
            Console.WriteLine($"\tСнимем асинхронно бронь сo столика {tableNumber} и оповестим вас");

            await Task.Run(async () =>
            {
                Table table = _tables.FirstOrDefault(t => t.Id == tableNumber);
                await Task.Delay(2000);

                if (table is null)
                {
                    InformManagementAboutNullProblem("Снятие брони асинхронно", tableNumber);
                }
                else if (table.State == EnumState.Free)
                {
                    _logger.LogDebug($"Restaurant DeleteBookingForTableAsync: стол #{tableNumber} и так свободен был");
                    Console.WriteLine($"Да этот стол #{tableNumber} и так свободен был, что вы нас от работы отвлекаете!");
                }
                else
                {
                    bool isSucces = table.SetState(EnumState.Free);

                    _logger.LogDebug($"Restaurant DeleteBookingForTableAsync Снятие брони с стола номер {table.Id} = {isSucces}");
                }
            });
        }

        /// <summary>
        /// Отправка уведомления руководству о проблеме
        /// </summary>
        /// <param name="action"></param>
        /// <param name="tableNumber"></param>
        private void InformManagementAboutNullProblem(string action, int tableNumber)
        {
            _logger.LogWarning($"Restaurant InformManagementAboutNullProblem Внимание! Что-то пошло не так при выполнении '{action}' для стола #{tableNumber}. ");

            Console.WriteLine($"Внимание! Что-то пошло не так при выполнении '{action}' для стола #{tableNumber}. " +
                $"Похоже у нас украли стол, так как вернулся null...");
        }

        /// <summary>
        /// Распечатать состояние всех столов
        /// </summary>
        internal void PrintTablesStatus()
        {
            Console.WriteLine("\tСостояние столиков:");
            foreach (Table table in _tables)
            {
                Console.WriteLine($"\t\tСтол#{table.Id} {table.State} \t{table.OrderId}");
            }
        }

        /// <summary>
        /// Автоматическое снятие брони со всех столиков по таймеру
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ResetAllTablesBooking(object? sender, ElapsedEventArgs e)
        {
            await Task.Run(async () =>
            {
                Console.WriteLine("Автоматическое снятие бронирования со всех столов");
                _logger.LogInformation($"Restaurant ResetAllTablesBooking Event: Автоматическое снятие бронирования со всех столов");

                foreach (Table table in _tables)
                {
                    if (table.State == EnumState.Booked)
                    {
                        bool isSucces = table.SetState(EnumState.Free);
                        _logger.LogDebug($"Restaurant ResetAllTablesBooking УВЕДОМЛЕНИЕ асинхронно! Снятие брони с стола номер {table.Id} = {isSucces}");
                    }
                }
            });
        }
    }
}