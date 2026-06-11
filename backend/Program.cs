using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TicketFlow")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("GetHealth");

app.Run();
