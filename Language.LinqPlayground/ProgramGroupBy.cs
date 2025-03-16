namespace Language.LinqPlayground;

using System.Data;
using System.Globalization;
using System.Linq;
using CsvHelper;
using Dapper;
using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

internal class ProgramGroupBy
{
    static async Task Main(string[] args)
    {
        PostgreSqlContainer _postgreSqlContainer = await StartPostgresContainer();

        Console.WriteLine(
            $"Postgres mapped dynamic Port is {_postgreSqlContainer.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort)}"
        );

        Console.WriteLine("Press any key to continue further to connect to database");

        Console.ReadLine();
        IEnumerable<TestCase> testCaseResults = null;
        await InitializeMigrations(_postgreSqlContainer, testCaseResults);
        ArgumentNullException.ThrowIfNull(testCaseResults, nameof(testCaseResults));

        var quoteWithAssertions = Map(testCaseResults);

        quoteWithAssertions
            .ToList()
            .ForEach(x =>
            {
                Console.WriteLine(x.QuoteId);
                Console.WriteLine("\t" + x.Quote);
                Console.Write("\t\t");
                Console.WriteLine(x.Assertions);
                Console.WriteLine();
            });

        await _postgreSqlContainer.DisposeAsync();
        //List<int> numbers = [35, 44, 200, 84, 3987, 4, 199, 329, 446, 208];

        //IEnumerable<IGrouping<int, int>> query = numbers.GroupBy(number => number % 2);

        //foreach (var group in query)
        //{
        //    Console.WriteLine(group.Key == 0 ? "\nEven numbers:" : "\nOdd numbers:");
        //    foreach (int i in group)
        //    {
        //        Console.WriteLine(i);
        //    }
        //}
    }

    private static async Task InitializeMigrations(
        PostgreSqlContainer _postgreSqlContainer,
        IEnumerable<TestCase> testCaseResults
    )
    {
        var _conn = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        Console.WriteLine("Postgres Connectionstring:");
        Console.WriteLine(_postgreSqlContainer.GetConnectionString());

        _conn.Open();

        await _conn.ExecuteAsync(SqlCreateTestCaseTable);

        var file = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestCaseDemo",
            "data-1727272166912.csv"
        );

        var stream = new StreamReader(file);
        var csv = new CsvReader(stream, CultureInfo.InvariantCulture);
        var testCases = csv.GetRecords<QuoteScenarioRow>().ToList();

        await _conn.ExecuteAsync(SqlInsertTestCases, testCases);

        testCaseResults = testCases.Select(x => MapToQuoteScenarioDomain(x));
    }

    private static async Task<PostgreSqlContainer> StartPostgresContainer()
    {
        PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
            .WithPortBinding(PostgreSqlBuilder.PostgreSqlPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(PostgreSqlBuilder.PostgreSqlPort)
            )
            .WithPassword("changeit")
            .Build();

        await _postgreSqlContainer.StartAsync();
        return _postgreSqlContainer;
    }

    public static IEnumerable<QuoteWithAssertions> Map(IEnumerable<TestCase> testRows) =>
        testRows
            .GroupBy(testCase => new
            {
                testCase.OriginCountryCode,
                testCase.OriginCity,
                testCase.OriginPostalCode,
                testCase.DestinationCountryCode,
                testCase.DestinationCity,
                testCase.DestinationPostalCode,
                testCase.NumberOfItems,
                testCase.ItemWeight,
                testCase.ItemLength,
                testCase.ItemWidth,
                testCase.ItemHeight,
                testCase.PackingType,
                testCase.ContentType,
            })
            .Select(group =>
            {
                var firstRow = group.FirstOrDefault();
                return firstRow == null
                    ? throw new Exception()
                    : new QuoteWithAssertions(
                        firstRow.ScenarioName,
                        new Quote(
                            firstRow.CustomerId,
                            firstRow.OriginCity,
                            firstRow.OriginPostalCode,
                            firstRow.OriginCountryCode,
                            firstRow.OriginAddressType,
                            firstRow.DestinationCity,
                            firstRow.DestinationPostalCode,
                            firstRow.DestinationCountryCode,
                            firstRow.DestinationAddressType,
                            [
                                new Item(
                                    firstRow.PackingType,
                                    firstRow.ContentType,
                                    firstRow.ItemWeight,
                                    firstRow.ItemLength,
                                    firstRow.ItemWidth,
                                    firstRow.ItemHeight,
                                    1
                                ),
                            ]
                        ),
                        group
                            .Select(x => new Assertion(
                                x.AssertionId,
                                x.ShouldBeReturned,
                                x.CarrierId,
                                x.CarrierServiceId,
                                x.Price
                            ))
                            .ToList()
                    );
            });

    private static TestCase MapToQuoteScenarioDomain(QuoteScenarioRow quoteScenarioRow)
    {
        return new TestCase(
            quoteScenarioRow.scenario_name,
            quoteScenarioRow.assertion_id,
            quoteScenarioRow.customer_id,
            quoteScenarioRow.origin_country_code,
            quoteScenarioRow.origin_city,
            quoteScenarioRow.origin_postal_code,
            quoteScenarioRow.origin_address_type,
            quoteScenarioRow.destination_country_code,
            quoteScenarioRow.destination_city,
            quoteScenarioRow.destination_postal_code,
            quoteScenarioRow.destination_address_type,
            quoteScenarioRow.number_of_items,
            quoteScenarioRow.item_length,
            quoteScenarioRow.item_width,
            quoteScenarioRow.item_weight,
            quoteScenarioRow.item_height,
            quoteScenarioRow.packing_type,
            quoteScenarioRow.content_type,
            quoteScenarioRow.carrier_id,
            quoteScenarioRow.carrier_service_id,
            quoteScenarioRow.should_be_returned,
            quoteScenarioRow.price
        );
    }

    private const string SqlCreateTestCaseTable =
        @"CREATE TABLE IF NOT EXISTS public.pricing_scenarios
(
    assertion_id text COLLATE pg_catalog.""default"" NOT NULL,
    scenario_name text COLLATE pg_catalog.""default"" NOT NULL,
    customer_id uuid NOT NULL,
    origin_country_code text COLLATE pg_catalog.""default"" NOT NULL,
    origin_city text COLLATE pg_catalog.""default"",
    origin_postal_code text COLLATE pg_catalog.""default"",
    origin_address_type text COLLATE pg_catalog.""default"",
    destination_country_code text COLLATE pg_catalog.""default"" NOT NULL,
    destination_city text COLLATE pg_catalog.""default"",
    destination_postal_code text COLLATE pg_catalog.""default"",
    destination_address_type text COLLATE pg_catalog.""default"",
    number_of_items bigint NOT NULL,
    item_length numeric(19,2) NOT NULL,
    item_width numeric(19,2) NOT NULL,
    item_weight numeric(19,2) NOT NULL,
    item_height numeric(19,2) NOT NULL,
    packing_type text COLLATE pg_catalog.""default"" NOT NULL,
    content_type text COLLATE pg_catalog.""default"" NOT NULL,
    carrier_id uuid NOT NULL,
    carrier_service_id uuid NOT NULL,
    should_be_returned boolean NOT NULL,
    price numeric(19,2) NOT NULL,
    CONSTRAINT ""PK_pricing_scenarios"" PRIMARY KEY (assertion_id, scenario_name)
)";
    private const string SqlInsertTestCases =
        "INSERT INTO pricing_scenarios(assertion_id, scenario_name, customer_id, origin_country_code, origin_city, origin_postal_code, origin_address_type, destination_country_code, destination_city, destination_postal_code, destination_address_type, number_of_items, item_length, item_width, item_weight, item_height, packing_type, content_type, carrier_id, carrier_service_id, should_be_returned, price) VALUES (@assertion_id, @scenario_name, @customer_id, @origin_country_code, @origin_city, @origin_postal_code, @origin_address_type, @destination_country_code, @destination_city, @destination_postal_code, @destination_address_type, @number_of_items, @item_length, @item_width,@item_weight, @item_height, @packing_type, @content_type, @carrier_id, @carrier_service_id, @should_be_returned, @price);";
}

public record TestCase(
    string ScenarioName,
    string AssertionId,
    Guid CustomerId,
    string OriginCountryCode,
    string OriginCity,
    string OriginPostalCode,
    string OriginAddressType,
    string DestinationCountryCode,
    string DestinationCity,
    string DestinationPostalCode,
    string DestinationAddressType,
    int NumberOfItems,
    decimal ItemLength,
    decimal ItemWidth,
    decimal ItemWeight,
    decimal ItemHeight,
    string PackingType,
    string ContentType,
    Guid CarrierId,
    Guid CarrierServiceId,
    bool ShouldBeReturned,
    decimal Price
);

public record QuoteScenarioRow(
    string assertion_id,
    string scenario_name,
    Guid customer_id,
    string origin_country_code,
    string origin_city,
    string origin_postal_code,
    string origin_address_type,
    string destination_country_code,
    string destination_city,
    string destination_postal_code,
    string destination_address_type,
    int number_of_items,
    decimal item_length,
    decimal item_width,
    decimal item_weight,
    decimal item_height,
    string packing_type,
    string content_type,
    Guid carrier_id,
    Guid carrier_service_id,
    bool should_be_returned,
    decimal price
)
{
    public QuoteScenarioRow()
        : this(
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default
        ) { }
}

public record QuoteWithAssertions(string QuoteId, Quote Quote, List<Assertion> Assertions);

public record Quote(
    Guid CustomerId,
    string OriginCity,
    string OriginPostalCode,
    string OriginCountryCode,
    string OriginAddressType,
    string DestinationCity,
    string DestinationPostalCode,
    string DestinationCountryCode,
    string DestinationAddressType,
    Item[] Items
);

public record Item(
    string PackingType,
    string ContentType,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    int Quantity = 1
);

public record Assertion(
    string Id,
    bool ShouldBeReturned,
    Guid CarrierId,
    Guid CarrierServiceId,
    decimal Price
);
