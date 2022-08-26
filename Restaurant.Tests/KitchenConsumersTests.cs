using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Interfaces;
using Repositories.Models;
using Restaurant.Booking.MassTransitDTO;
using Restaurant.Kitchen.Consumers;
using Restaurant.Kitchen.Models;
using Restaurant.Messages.Interfaces;


namespace Restaurant.Tests
{
    internal class KitchenConsumersTests
    {
        private ServiceProvider _provider;
        private ITestHarness _harness;

        [OneTimeSetUp]
        public async Task Init()
        {
            _provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<KitchenRequestedConsumer>();
                })
                .AddLogging()
                .AddTransient<Kitchen.Manager>()
                .AddSingleton<IDataBaseRepositoty<TableBookedModel>, Repositoties.KitchenIdempotencytRepository>()
                .BuildServiceProvider(true);

            _harness = _provider.GetTestHarness();

            // !!! подложить файл с БД с созданными таблицами в папку debug тестов. Или прописать путь !!!
            RepositoryConnectionSettings.ConnectionString = "Data Source=idempotency.db;Version=3;Pooling=true;Max Pool Size=100;";

            await _harness.Start();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _harness.OutputTimeline(TestContext.Out, options => options.Now().IncludeAddress());
            await _provider.DisposeAsync();
        }

        [Test]
        public async Task Any_KitchenRequestedConsumer_Request_Consumed()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((ITableBooked)
                new TableBooked(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 }));

            var selected = _harness.Published.Select<ITableBooked>().FirstOrDefault();

            Assert.That(await _harness.Consumed
                .Any<ITableBooked>());
        }

        [Test]
        public async Task KitchenRequestedConsumer_Request_Consumed_ExactMessage()
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((ITableBooked)
                new TableBooked(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 }));

            Assert.That(await _harness.Consumed
                .Any<ITableBooked>(x =>
                    x.Context.Message.OrderId == orderId),
                    Is.True);
        }

        [Test]
        public async Task KitchenRequestedConsumer_publish_KitchenReady_message()
        {
            var orderId = NewId.NextGuid();
            var clientId = Guid.NewGuid();

            await _harness.Bus.Publish((ITableBooked)
                new TableBooked(
                    orderId,
                    clientId,
                    new Messages.Dish { Id = 1 }));

            Assert.That(_harness.Consumed.Select<ITableBooked>()
                .Any(x =>
                    x.Context.Message.OrderId == orderId),
                    Is.True);

            Assert.That(_harness.Published.Select<IKitchenReady>()
                .Any(x =>
                    x.Context.Message.OrderId == orderId),
                    Is.True);
        }
    }
}
