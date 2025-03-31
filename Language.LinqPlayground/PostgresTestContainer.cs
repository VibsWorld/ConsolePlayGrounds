using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace Language.LinqPlayground
{
    public class PostgresTestContainer
    {
        private Task _initializationTask = null!;
        public PostgreSqlContainer GetPostgreSqlContainer { get; private set; }
        public PostgresTestContainer()
        {
            _initializationTask = StartPostgresContainer();
        }

        public async Task StartContainer()
        {
            await _initializationTask;
        }

        private async Task StartPostgresContainer()
        {
            GetPostgreSqlContainer = new PostgreSqlBuilder()
                .WithPortBinding(PostgreSqlBuilder.PostgreSqlPort, true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer().UntilPortIsAvailable(PostgreSqlBuilder.PostgreSqlPort)
                )
                .WithPassword("changeit")
                .Build();

            await GetPostgreSqlContainer.StartAsync();
           
        }

    }
}
