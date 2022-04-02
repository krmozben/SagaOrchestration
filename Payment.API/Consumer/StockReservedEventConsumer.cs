using MassTransit;
using Shared;

namespace Payment.API.Consumer
{
    public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<StockReservedEventConsumer> _logger;

        public StockReservedEventConsumer(IPublishEndpoint publishEndpoint, ILogger<StockReservedEventConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StockReservedEvent> context)
        {
            var balance = 300m;
            if (balance > context.Message.Payment.TotalPrice)
            {
                _logger.LogInformation($"{context.Message.Payment.TotalPrice} TL was withdrawn from creadit card for user id = {context.Message.BuyerId}");

                await _publishEndpoint.Publish(new PaymentCompletedEvent
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId
                });
            }
            else
            {
                _logger.LogInformation($"{context.Message.Payment.TotalPrice} TL was not withdrawn from creadit card for user id = {context.Message.BuyerId}");

                await _publishEndpoint.Publish(new PaymentFailedEvent
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    Message = "Not enough balance",
                    OrderItems = context.Message.OrderItems
                });
            }
        }
    }
}
