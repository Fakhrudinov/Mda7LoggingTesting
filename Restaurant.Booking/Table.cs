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

        //unbook
        public bool SetState(EnumState state)
        {
            if (state == State)
                return false;

            State = state;
            OrderId = null;

            return true;
        }

        //book
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