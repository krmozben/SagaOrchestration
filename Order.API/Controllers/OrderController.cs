using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs;
using Order.API.Models;
using Shared;

namespace Order.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderController(AppDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateDto orderCreate)
        {
            var newOrder = new Models.Order
            {
                BuyerId = orderCreate.BuyerId,
                Status = OrderStatus.Suspend,
                Address = new Address { Line = orderCreate.Address.Line, District = orderCreate.Address.District, Province = orderCreate.Address.Province },
                CreatedDate = DateTime.Now
            };

            orderCreate.OrderItems.ForEach(item =>
            {
                newOrder.Items.Add(new OrderItem() { Price = item.Price, ProductId = item.ProductId, Count = item.Count });
            });

            await _context.AddAsync(newOrder);
            await _context.SaveChangesAsync();

            OrderCreatedEvent orderCreatedEvent = new OrderCreatedEvent()
            {
                BuyerId = orderCreate.BuyerId,
                OrderId = newOrder.Id,
                Payment = new PaymentMessage
                {
                    CardName = orderCreate.Payment.CardName,
                    CardNumber = orderCreate.Payment.CardNumber,
                    CVV = orderCreate.Payment.CVV,
                    Expiration = orderCreate.Payment.Expiration,
                    TotalPrice = orderCreate.OrderItems.Sum(s => s.Price * s.Count),
                }
            };

            orderCreate.OrderItems.ForEach(item =>
            {
                orderCreatedEvent.OrderItems.Add(new OrderItemMessage() { ProductId = item.ProductId, Count = item.Count });
            });

            await _publishEndpoint.Publish(orderCreatedEvent);

            return Ok();
        }
    }
}