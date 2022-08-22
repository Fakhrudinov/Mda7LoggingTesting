using Automatonymous;
using MassTransit;
using Restaurant.Booking.Consumers.Interfaces;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Messages.Interfaces;
using System;


namespace Restaurant.Booking.Saga
{
    public sealed class RestaurantBookingSaga : MassTransitStateMachine<RestaurantBooking>
    {
        public State AwaitingBookingFullyApproved { get; private set; }
        public State AwaitingGuestArrived { get; private set; }

        public Event<IBookingRequest> BookingRequestedEvent { get; private set; }
        public Event<ITableBooked> TableBookedEvent { get; private set; }
        public Event<IBookingReject> BookingRejectEvent { get; private set; }
        public Event<IKitchenReady> KitchenReadyEvent { get; private set; }
        public Event<IKitchenReject> KitchenRejectEvent { get; private set; }
        public Event BookingApprovedEvent { get; private set; }
        public Event<Fault<IBookingRequest>> BookingRequestFaultEvent { get; private set; }

        public Event<Fault<ITableBooked>> KitchenFaultEvent { get; private set; }
        public Event<Fault<INotify>> NotificationFaultEvent { get; private set; }

        public Schedule<RestaurantBooking, IBookingExpired> BookingExpiredSchedule { get; private set; }
        public Schedule<RestaurantBooking, IGuestWaitingExpired> BookingAwaitingGuestSchedule { get; private set; }
        public Schedule<RestaurantBooking, IGuestArrived> ActualGuestArrivalSchedule { get; private set; }


        public RestaurantBookingSaga()
        {
            InstanceState(x => x.CurrentState);

            
            Event(() => BookingRequestedEvent,
                x =>
                    x.CorrelateById(context => context.Message.OrderId)
                        .SelectId(context => context.Message.OrderId));

            Event(() => TableBookedEvent,
                x =>
                    x.CorrelateById(context => context.Message.OrderId));

            Event(() => BookingRejectEvent,
                x =>
                    x.CorrelateById(context => context.Message.OrderId));

            Event(() => BookingRequestFaultEvent,
                x =>
                    x.CorrelateById(m => m.Message.Message.OrderId));
            Event(() => KitchenFaultEvent,
                x =>
                    x.CorrelateById(m => m.Message.Message.OrderId));
            Event(() => NotificationFaultEvent,
                x =>
                    x.CorrelateById(m => m.Message.Message.OrderId));

            CompositeEvent(() => BookingApprovedEvent,
                x => x.ReadyEventStatus, KitchenReadyEvent, TableBookedEvent);


            Event(() => KitchenRejectEvent,
                x =>
                    x.CorrelateById(context => context.Message.OrderId));

            Event(() => KitchenReadyEvent,
                x =>
                    x.CorrelateById(context => context.Message.OrderId));



            Schedule(() => BookingExpiredSchedule,
                x => x.BookingExpirationId, x =>
                {
                    x.Delay = TimeSpan.FromSeconds(10);
                    x.Received = e => e.CorrelateById(context => context.Message.OrderId);
                });

            Schedule(() => ActualGuestArrivalSchedule,
                х => х.GuestArrivalId,
                x =>
                {
                    x.Delay = TimeSpan.FromSeconds(1);
                    x.Received = e => e.CorrelateById(context => context.Message.OrderId);
                });

            Schedule(() => BookingAwaitingGuestSchedule,
                х => х.GuestAwaitingId,
                x =>
                {
                    x.Delay = TimeSpan.FromSeconds(1);
                    x.Received = e => e.CorrelateById(context => context.Message.OrderId);
                });


            Initially(
                When(BookingRequestedEvent)
                    .Then(context =>
                    {
                        context.Instance.CorrelationId = context.Data.OrderId;
                        context.Instance.OrderId = context.Data.OrderId;
                        context.Instance.ClientId = context.Data.ClientId;
                        context.Instance.BookingArrivalTime = context.Data.BookingArrivalTime;
                        context.Instance.ActualArrivalTime = context.Data.ActualArrivalTime;
                        Console.WriteLine($"Saga BookingRequestedEvent order={context.Data.OrderId} client={context.Data.ClientId}");
                    })
                    .Schedule(
                        BookingExpiredSchedule,
                        context => 
                            new BookingExpired(context.Instance))
                    .TransitionTo(AwaitingBookingFullyApproved)
            );


            During(AwaitingBookingFullyApproved,
                When(BookingApprovedEvent)
                    .Unschedule(BookingExpiredSchedule)
                    .Publish(context =>
                        (INotify)new Notify(
                            context.Instance.OrderId,
                            context.Instance.ClientId,
                            $"Saga AwaitingBookingApproved Стол успешно забронирован"))
                    .Then(context => 
                        Console.WriteLine($"Saga AwaitingBookingApproved Ожидание гостя {context.Instance.ClientId} " +
                        $"actual=>{TimeSpan.FromSeconds(context.Instance.ActualArrivalTime)} " +
                        $"booking=>{TimeSpan.FromSeconds(context.Instance.BookingArrivalTime)}"))
                
                    .Schedule(ActualGuestArrivalSchedule, 
                        context => new GuestArrived(context.Instance),
                        context => TimeSpan.FromSeconds(context.Instance.ActualArrivalTime))

                    .Schedule(BookingAwaitingGuestSchedule, 
                        context => new GuestWaitingExpired(context.Instance),
                        context => TimeSpan.FromSeconds(context.Instance.BookingArrivalTime))
                    .TransitionTo(AwaitingGuestArrived),

                When(BookingRequestFaultEvent)//при эксепшене в IBookingRequested
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Then(context => 
                        Console.WriteLine($"Ошибочка вышла!"))
                    .Publish(context => (INotify)new Notify(
                        context.Instance.OrderId,
                        context.Instance.ClientId,
                        $"Saga BookingRequestFaultEvent Приносим извинения, стол забронировать не получилось."))
                    .Publish(context => (IBookingCancelRequested)
                        new BookingCancell(context.Instance.OrderId))
                    .Finalize(),

               When(KitchenFaultEvent)//при эксепшене в ITableBooked // kitchen
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Then(context =>
                        Console.WriteLine("Saga KitchenFaultEvent На кухне произошла ошибка!"))
                    .Publish(context => (INotify)new Notify(
                        context.Instance.OrderId,
                        context.Instance.ClientId,
                        $"Saga KitchenFaultEvent Отмена кухни по заказу #{context.Instance.OrderId} в связи с отсутсвием блюда!"))
                    .Publish(context => (IBookingCancelRequested)
                        new BookingCancell(context.Instance.OrderId))
                    .Finalize(),

                When(NotificationFaultEvent)//при эксепшене в INotify
                    .Then(context => 
                        Console.WriteLine("Saga NotificationFaultEvent В сервисе уведомлений произошла ошибка!"))
                    .Finalize(),


                When(BookingRejectEvent)//1 когда все столы заняты
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Publish(context => (INotify)new Notify(
                        context.Instance.OrderId,
                        context.Instance.ClientId,
                        $"Saga BookingRejectEvent Приносим извинения, стол забронировать не получилось. #{context.Instance.OrderId} в связи с отсутсвием свободного стола!"))
                    .Finalize(),

                 When(KitchenRejectEvent)//2
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Publish(context => (INotify)new Notify(
                        context.Instance.OrderId,
                        context.Instance.ClientId,
                        $"Saga KitchenRejectEvent Отмена кухни по заказу #{context.Instance.OrderId} в связи с отсутсвием блюда!"))
                    .Publish(context => (IBookingCancelRequested)
                        new BookingCancell(context.Instance.OrderId))
                    .Finalize(),

                When(BookingExpiredSchedule?.Received)//1 из за кухни
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Then(context => 
                        Console.WriteLine($"Saga BookingExpiredSchedule.Received Отмена заказа - тормозим на кухне #{context.Instance.OrderId}  " +
                        $"ActualArrivalTime= {context.Instance.ActualArrivalTime} BookingArrivalTime={context.Instance.BookingArrivalTime}"))
                    .Publish(context => (INotify)new Notify(
                        context.Instance.OrderId,
                        context.Instance.ClientId,
                        $"Saga BookingExpiredSchedule.Received Отмена заказа #{context.Instance.OrderId} в связи с задержкой на кухне!"))
                    .Publish(context => (IBookingCancelRequested)
                        new BookingCancell(context.Instance.OrderId))
                    .Finalize()
            );


            During(AwaitingGuestArrived,//1
                When(ActualGuestArrivalSchedule?.Received)
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Then(context => 
                        Console.WriteLine($"Saga ActualGuestArrivalSchedule?.Received Гость ClientId={context.Instance.ClientId} прибыл, " +
                        $"ActualArrivalTime= {context.Instance.ActualArrivalTime} BookingArrivalTime={context.Instance.BookingArrivalTime}"))
                    .Finalize(),

                When(BookingAwaitingGuestSchedule?.Received)//3
                    .Unschedule(BookingExpiredSchedule)
                    .Unschedule(ActualGuestArrivalSchedule)
                    .Unschedule(BookingAwaitingGuestSchedule)
                    .Then(context => 
                        Console.WriteLine($"Saga Гость ClientId={context.Instance.ClientId} не пришел"))
                    .Publish(context => (INotify)new Notify(
                            context.Instance.OrderId,
                            context.Instance.ClientId,
                            $"Saga BookingAwaitingGuestSchedule?.Received Отмена заказа #{context.Instance.OrderId} - вы не пришли!"))
                    .Publish(context => (IBookingCancelRequested)
                        new BookingCancell(context.Instance.OrderId))
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }
    }
}
