using AutofacModule;
using Lycoris.Autofac.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.UseAutofacExtensions(builder =>
{
    builder.EnabledLycorisMultipleService = true;
    builder.AddLycorisRegisterModule<ApplicationModule>();
});

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
