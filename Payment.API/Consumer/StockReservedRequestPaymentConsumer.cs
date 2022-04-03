using MassTransit;
using Shared.Events;
using Shared.Interfaces;

namespace Payment.API.Consumer
{
    public class StockReservedRequestPaymentConsumer : IConsumer<IStockReservedRequestPayment>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<StockReservedRequestPaymentConsumer> _logger;

        public StockReservedRequestPaymentConsumer(IPublishEndpoint publishEndpoint, ILogger<StockReservedRequestPaymentConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IStockReservedRequestPayment> context)
        {
            var balance = 300m;
            if (balance > context.Message.Payment.TotalPrice)
            {
                _logger.LogInformation($"{context.Message.Payment.TotalPrice} TL was withdrawn from creadit card for user id = {context.Message.BuyerId}");

                await _publishEndpoint.Publish(new PaymentCompletedEvent(context.Message.CorrelationId));
            }
            else
            {
                _logger.LogInformation($"{context.Message.Payment.TotalPrice} TL was not withdrawn from creadit card for user id = {context.Message.BuyerId}");

                await _publishEndpoint.Publish(new PaymentFailedEvent(context.Message.CorrelationId)
                {
                    Reason = "Not enough balance",
                    OrderItems = context.Message.OrderItems
                });
            }
        }
    }
}
