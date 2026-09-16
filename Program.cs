using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HobbyCollector;
using HobbyCollector.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<LocalEmbeddingService>();
builder.Services.AddScoped<WasmVectorStore>();
builder.Services.AddScoped<WasmCollectorService>();

await builder.Build().RunAsync();
