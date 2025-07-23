
using Bogus;
using Bogus.DataSets;
using WebApiPdfReporting.Models;

namespace WebApiPdfReporting;

internal sealed class InvoiceFactory
{
    public Invoice Create(int numberOfLinesItems = 10)
    {
        var faker = new Faker();

        return new Invoice
        {
            Number = faker.Random.Number(100_000, 1_000_000).ToString(),
            IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            SellerAddress = new Models.Address
            {
                CompanyName = faker.Company.CompanyName(),
                Street = faker.Address.StreetAddress(),
                City = faker.Address.City(),
                State = faker.Address.State(),
                Email = faker.Internet.Email()
            },
            CustomerAddress = new Models.Address
            {
                CompanyName = faker.Company.CompanyName(),
                Street = faker.Address.StreetAddress(),
                City = faker.Address.City(),
                State = faker.Address.State(),
                Email = faker.Internet.Email()
            },
            LineItems = Enumerable
            .Range(1, numberOfLinesItems)
            .Select(i => new LineItem
            {
                Id = i,
                Name = faker.Commerce.ProductName(),
                Price = faker.Random.Decimal(1, 1000),
                Quantity = faker.Random.Int(1, 10)
            }).ToList()
        };
    }
}