using System;

namespace Lesson1
{
    public class Table
    {
        public EnumState State { get; private set; }
        public int SeatsCount { get; }
        public int Id { get; }
        public Guid? OrderId { get; private set; }

        public Table(int id)
        {
            Id = id; //в учебном примере просто присвоим id при вызове
            State = EnumState.Free; // новый стол всегда свободен
            SeatsCount = Random.Next(2, 5); //пусть количество мест за каждым столом будет случайным, от 2х до 5ти
            OrderId = null;
        }

        /// <summary>
        /// Выставление статуса свободен для стола
        /// </summary>
        /// <param name="state">Enum</param>
        /// <returns>Bool</returns>
        public bool SetState(EnumState state)
        {
            if (state == State)
                return false;

            State = state;
            OrderId = null;

            return true;
        }

        /// <summary>
        /// Выставление статуса занят для стола
        /// </summary>
        /// <param name="state">Enum</param>
        /// <param name="orderId">Номер заказа</param>
        /// <returns>Bool</returns>
        public bool SetState(EnumState state, Guid orderId)
        {
            if (state == State)
                return false;

            State = state;
            OrderId = orderId;

            return true;
        }

        private static readonly Random Random = new ();        
    }
}