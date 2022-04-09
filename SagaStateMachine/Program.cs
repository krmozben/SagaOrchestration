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

                ///Yukarýdaki kod SAGA yý ayaða kaldýrmaya yeter. Ancak saga nýn dinlemesi gereken Event lerden biri IOrderCreatedRequestEvent olduðu için ve bu Event Send metod ile "order-saga-queue" kuyruðuna gönderildiði için SAGA nýn aþaðýda görüleceði gibi bu kuðruðu dinlemesini istiyoruz.
                ///SAGA birden çok kuyruðu dinleyebilir.
                //ÖRNEÐÝN;
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
