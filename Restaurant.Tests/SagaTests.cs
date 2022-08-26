using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Booking.Models;
using Restaurant.Kitchen;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Messages.Repositories.Interfaces;
using MassTransit;
using Restaurant.Booking.Saga;
using Restaurant.Messages.Interfaces;
using Restaurant.Kitchen.Consumers;
using Restaurant.Booking.Consumers;
using Restaurant.Booking.MassTransitDTO;
using Repositories.Interfaces;
using Restaurant.Kitchen.Models;
using Repositories.Models;
using Restaurant.Booking.Consumers.Interfaces;

namespace Restaurant.Tests
{
    [TestFixture]
    public class SagaTests
    {
        //[OneTimeSetUp]
        [SetUp]
        public async Task Init()
        {
            _provider = new ServiceCollection()
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<KitchenRequestedConsumer>();
                    cfg.AddConsumer<BookingRequestConsumer>();
                    cfg.AddConsumer<BookingCancelRequested>();

                    cfg.AddSagaStateMachine<RestaurantBookingSaga, RestaurantBooking>()
                        .InMemoryRepository();
                    cfg.AddSagaStateMachineTestHarness<RestaurantBookingSaga, RestaurantBooking>();
                    cfg.AddDelayedMessageScheduler();
                })
                .AddLogging()
                .AddTransient<Booking.Restaurant>()
                .AddTransient<Manager>()
                .AddSingleton<IInMemoryRepository<BookingRequestModel>, InMemoryRepository<BookingRequestModel>>()
                .AddSingleton<IDataBaseRepositoty<TableBookedModel>, Repositoties.KitchenIdempotencytRepository>()
                .BuildServiceProvider(true);

            _harness = _provider.GetRequiredService<InMemoryTestHarness>();

            _harness.OnConfigureInMemoryBus += configurator => configurator.UseDelayedMessageScheduler();

            // !!! подложить файл с БД с созданными таблицами в папку debug тестов. Или прописать путь !!!
            RepositoryConnectionSettings.ConnectionString = "Data Source=idempotency.db;Version=3;Pooling=true;Max Pool Size=100;";

            await _harness.Start();
        }

        private ServiceProvider _provider;
        private InMemoryTestHarness _harness;

        [TearDown]
        public async Task OneTimeTearDown()
        {
            await _harness.Stop();
            await _provider.DisposeAsync();
        }

        [Test]
        public async Task NormalBooking_Gets_Normal_Responses()
        {
            var orderId = NewId.NextGuid();
            var clientId = NewId.NextGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 },
                    15,
                    1));

            Assert.That(await _harness.Published.Any<IBookingRequest>());
            Assert.That(await _harness.Consumed.Any<IBookingRequest>());

            var sagaHarness = _provider
                .GetRequiredService<ISagaStateMachineTestHarness<RestaurantBookingSaga, RestaurantBooking>>();

            Assert.That(await sagaHarness.Consumed.Any<IBookingRequest>());
            Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));

            RestaurantBooking saga = sagaHarness.Created.Contains(orderId);

            Assert.That(saga, Is.Not.Null);
            Assert.That(saga.ClientId, Is.EqualTo(clientId));

            Assert.That(await _harness.Published.Any<ITableBooked>());
            Assert.That(await _harness.Published.Any<IKitchenReady>());
            Assert.That(await _harness.Published.Any<INotify>());

            Assert.That(saga.CurrentState, Is.EqualTo(3));
        }


        [Test]
        public async Task WrongDishBooking_Gets_Reject_Responses()
        {
            var orderId = NewId.NextGuid();
            var clientId = NewId.NextGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 4 },
                    15,
                    1));

            Assert.That(await _harness.Published.Any<IBookingRequest>());
            Assert.That(await _harness.Consumed.Any<IBookingRequest>());

            var sagaHarness = _provider
                .GetRequiredService<ISagaStateMachineTestHarness<RestaurantBookingSaga, RestaurantBooking>>();

            Assert.That(await sagaHarness.Consumed.Any<IBookingRequest>());
            Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));

            RestaurantBooking saga = sagaHarness.Created.Contains(orderId);

            Assert.That(saga, Is.Not.Null);
            Assert.That(saga.ClientId, Is.EqualTo(clientId));

            Assert.That(await _harness.Published.Any<ITableBooked>());
            Assert.That(await _harness.Published.Any<IBookingCancelRequested>());
            Assert.That(await _harness.Published.Any<INotify>());

            Assert.That(_harness.Published.Select<INotify>()
                .Any(x =>
                    x.Context.Message.Message.Contains("в связи с отсутсвием блюда")),
                    Is.True);
        }

        [Test]
        public async Task WrongGuestArrivalTime_Gets_Reject_Responses()
        {
            var orderId = NewId.NextGuid();
            var clientId = NewId.NextGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 },
                    1,
                    15));

            Assert.That(await _harness.Published.Any<IBookingRequest>());
            Assert.That(await _harness.Consumed.Any<IBookingRequest>());

            var sagaHarness = _provider
                .GetRequiredService<ISagaStateMachineTestHarness<RestaurantBookingSaga, RestaurantBooking>>();

            Assert.That(await sagaHarness.Consumed.Any<IBookingRequest>());
            Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));

            RestaurantBooking saga = sagaHarness.Created.Contains(orderId);

            Assert.That(saga, Is.Not.Null);
            Assert.That(saga.ClientId, Is.EqualTo(clientId));

            Assert.That(await sagaHarness.Consumed.Any<IGuestWaitingExpired>());
            Assert.That(await _harness.Published.Any<ITableBooked>());
            Assert.That(await _harness.Published.Any<IBookingCancelRequested>());
            Assert.That(await _harness.Published.Any<INotify>());

            Assert.That(_harness.Published.Select<INotify>()
                .Any(x =>
                    x.Context.Message.Message.Contains("вы не пришли")),
                    Is.True);
        }
    }
}
