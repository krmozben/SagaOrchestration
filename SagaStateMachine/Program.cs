using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine;
using SagaStateMachine.Model;
using Shared;
using System.Reflection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMassTransit(cfg =>
        {
            cfg.AddSagaStateMachine<OrderStateMachine, OrderState>().EntityFrameworkRepository(opt =>
            {
                opt.AddDbContext<DbContext, OrderStateDbContext>((provider, builder) =>
                {
                    builder.UseSqlServer(hostContext.Configuration.GetConnectionString("SqlCon"), m =>
                    {
                        m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                    });
                });
            });

            cfg.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(mq =>
            {
                mq.Host("localhost", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });


                //mq.ReceiveEndpoint(e =>
                //{
                //    e.ConfigureSaga<OrderState>(provider);
                //});

                ///Yukar�daki kod SAGA y� aya�a kald�rmaya yeter. Ancak saga n�n dinlemesi gereken Event lerden biri IOrderCreatedRequestEvent oldu�u i�in ve bu Event Send metod ile "order-saga-queue" kuyru�una g�nderildi�i i�in SAGA n�n a�a��da g�r�lece�i gibi bu ku�ru�u dinlemesini istiyoruz.
                ///SAGA birden �ok kuyru�u dinleyebilir.
                //�RNE��N;
                //mq.ReceiveEndpoint("kuruk-adi", e =>
                //{
                //    e.ConfigureSaga<OrderState>(provider);
                //});

                mq.ReceiveEndpoint(RabbitMQSettingsConst.OrderSaga, e =>
                {
                    e.ConfigureSaga<OrderState>(provider);
                });

            }));
        });
    })
    .Build();

await host.RunAsync();
