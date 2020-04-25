﻿using ExternalDependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SeatsSuggestions.Domain;
using SeatsSuggestions.Domain.Ports;
using SeatsSuggestions.Infra;
using SeatsSuggestions.Infra.Adapter;
using SeatsSuggestions.Infra.Helpers;
using Swashbuckle.AspNetCore.Swagger;

namespace SeatsSuggestions.Api
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            ConfigurePortsAndAdapters(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "SeatsSuggestions API", Version = "v1" });
            });
        }

        private static void ConfigurePortsAndAdapters(IServiceCollection services)
        {
            // The 3 steps initialization of the Hexagonal Architecture 
            // Step1: Instantiate the "I want to go out" (i.e. right-side) adapters
            var webClient = new WebClient();
            services.AddSingleton<IWebClient>(webClient);

            IProvideAuditoriumLayouts auditoriumSeatingRepository = new AuditoriumWebRepository("http://localhost:50950/", webClient);
            IProvideCurrentReservations seatReservationsProvider = new SeatReservationsWebRepository("http://localhost:50951/", webClient);

            var auditoriumSeatingAdapter = new AuditoriumSeatingAdapter(auditoriumSeatingRepository, seatReservationsProvider);

            // Step2: Instantiate the hexagon
            var hexagon = new SeatAllocator(auditoriumSeatingAdapter);
            services.AddSingleton<IRequestSuggestions>(hexagon);

            // Step3: Instantiate the "I want to go in" (i.e. left-side) adapters
            // ... actually, this will be done everytime the Left Adapter (SeatsSuggestionsController) will be instantiated by ASP.NET.
            // It will receive the Hexagon (i.e. the SeatAllocator instance)
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeatsSuggestions API v1");
            });

            app.UseMvc();
        }
    }
}
