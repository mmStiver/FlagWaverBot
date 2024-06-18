using PeriodicBotRunner;
using FlagWaver.Core;
using FlagWaver.Core.Service;


using System.Net;
using DiscuitSharp.Core.Auth;
using PeriodicBotRunner.Options;


HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);

var c = appBuilder.Configuration;
appBuilder.Services.Configure<EnvCredential>( cr => {
  cr.UserName = c["Credentials:UserName"] ?? string.Empty;
  cr.Password = c["Credentials:Password"] ?? string.Empty;
});
appBuilder.Services.AddHttpClient("DisctuitHttpClient");
appBuilder.Services.AddScoped<IFlagWaverService, FlagWaverService>();
appBuilder.Services.AddHostedService<PeriodicHostedService>();

var host = appBuilder.Build();
host.Run();



