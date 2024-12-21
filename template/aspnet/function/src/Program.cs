using Microsoft.AspNetCore.Builder;
using OpenFaaS.Hosting;

Runner.Run( args, builder =>
{
    // add your services to the container
}, app =>
{
    // configure the HTTP request pipeline

    app.MapPost( "/", () =>
    {
        return new
        {
            Message = "Hello"
        };
    } );
} );
