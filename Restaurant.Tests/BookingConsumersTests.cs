using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using Restaurant.Booking.Consumers;
using Restaurant.Messages.Repositories.Interfaces;
using Restaurant.Messages.Interfaces;
using Restaurant.Messages.Repositories.Implementation;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Booking.Models;

namespace Restaurant.Tests
{
    [TestFixture]
    public class BookingConsumersTests
    {
        private ServiceProvider _provider;
        private ITestHarness _harness;

        [OneTimeSetUp]
        public async Task Init()
        {
            _provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<BookingRequestConsumer>(); 
                    cfg.AddConsumer<BookingCancelRequested>(); 
                })
                .AddLogging()
                .AddTransient<Booking.Restaurant>()
                .AddSingleton<IInMemoryRepository<BookingRequestModel>, InMemoryRepository<BookingRequestModel>>()
                .BuildServiceProvider(true);

            _harness = _provider.GetTestHarness();

            await _harness.Start();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _harness.OutputTimeline(TestContext.Out, options => options.Now().IncludeAddress());
            await _provider.DisposeAsync();            
        }


        [Test]
        public async Task Any_BookingRequested_Request_Consumed()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 },
                    15,
                    5));

            Assert.That(await _harness.Consumed
                .Any<IBookingRequest>());
        }

        [Test]
        public async Task Booking_request_consumer_published_table_booked_message()
        {
            var orderId = NewId.NextGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 },
                    15,
                    5));

            Assert.That(_harness.Consumed.Select<IBookingRequest>()
                .Any(x => 
                    x.Context.Message.OrderId == orderId), 
                    Is.True);

            Assert.That(_harness.Published.Select<ITableBooked>()
                .Any(x => 
                    x.Context.Message.OrderId == orderId), 
                    Is.True);
        }

        [Test]
        public async Task BookingRequested_Request_Consumed_ExactMessage()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((IBookingRequest)
                new BookingRequested(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 },
                    15,
                    1));

            Assert.That(await _harness.Consumed
                .Any<IBookingRequest>(x =>
                    x.Context.Message.OrderId == orderId),
                    Is.True);
        }

        [Test]
        public async Task Any_BookingCancelRequested_Request_Consumed()
        {
            var orderId = Guid.NewGuid();

            await _harness.Bus.Publish<IBookingCancelRequested>(
                new BookingCancell(orderId));

            Assert.That(await _harness.Consumed
                .Any<IBookingCancelRequested>(x => 
                    x.Context.Message.OrderId == orderId), 
                    Is.True);
        }
    }
}