using MessageBroker;
using MessageBroker.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSqliteDbContext(builder.Configuration);

var app = builder.Build();

app.RegisterTopicsEndpoints();
app.RegisterSubscriptionsEndpoints();

app.Run();
