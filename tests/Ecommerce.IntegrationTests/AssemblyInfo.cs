// Cada classe de teste sobe seu próprio WebApplicationFactory<Program> (host ASP.NET Core
// completo, em processo). Rodar mais de um em paralelo faz dois hosts disputarem o logger
// estático do Serilog (Log.Logger / Log.CloseAndFlush em Program.cs) — um teste fecha o logger
// enquanto o outro ainda está subindo, e a exceção é engolida pelo catch-all do Program.cs
// (sintoma: "The entry point exited without ever building an IHost"). Roda sequencial aqui pra
// evitar esse tipo de flakiness — o custo de velocidade é aceitável pro tamanho atual da suíte.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
