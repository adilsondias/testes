using Amazon.SQS;
using Teste.SQSConsumer.Worker;
using Teste.SQSConsumer.Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<AWSOptions>(builder.Configuration.GetSection("AWS"));

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

builder.Services.AddHostedService<SqsWorker>();

var host = builder.Build();
host.Run();
