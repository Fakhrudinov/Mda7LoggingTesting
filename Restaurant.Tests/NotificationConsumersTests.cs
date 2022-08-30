using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Messages.Interfaces;
using Restaurant.Notification.Consumers;

namespace Restaurant.Tests
{
    internal class NotificationConsumersTests
    {
        private ServiceProvider _provider;
        private ITestHarness _harness;

        [OneTimeSetUp]
        public async Task Init()
        {
            _provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<NotifyConsumer>();
                })
                .AddLogging()
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
        public async Task Any_NotifyConsumer_Request_Consumed()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((INotify)
                new Notify(
                    orderId,
                    clientId,
                    "OK"));

            Assert.That(await _harness.Consumed
                .Any<INotify>());
        }

        [Test]
        public async Task NotifyConsumer_Request_Consumed_ExactMessage()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((INotify)
                new Notify(
                    orderId,
                    clientId,
                    "OK2"));

            Assert.That(await _harness.Consumed
                .Any<INotify>(x => 
                    x.Context.Message.OrderId == orderId), 
                    Is.True);
        }
    }
}
