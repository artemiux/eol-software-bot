using EolBot.Services.Report.Provider.EndoflifeDate;
using EolBot.Services.Report.Provider.EndoflifeDate.Api;
using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using Moq;

namespace EolBot.Tests.Report
{
    public class EndoflifeDateProviderTests
    {
        [Fact]
        public async Task Get_FindsExactlyTwoItems()
        {
            Release productARelease = new()
            {
                Name = "1.0",
                Label = "1.0",
                IsMaintained = true,
                IsEol = false,
                EolFrom = new DateTime(2199, 1, 1, 0, 0, 0)
            };
            FullProduct productA = new()
            {
                Name = "producta",
                Label = "Product A",
                Links = new Link
                {
                    Html = $"https://endoflife.date/product-a"
                },
                Releases = [productARelease]
            };
            Release productBRelease = new()
            {
                Name = "2.0",
                Label = "2.0",
                IsMaintained = true,
                IsEol = false,
                IsEoas = false,
                EoasFrom = new DateTime(2199, 1, 6, 0, 0, 0)
            };
            FullProduct productB = new()
            {
                Name = "productb",
                Label = "Product B",
                Links = new Link
                {
                    Html = $"https://endoflife.date/productb"
                },
                Releases = [productBRelease]
            };
            var mockEndoflifeDateClient = new Mock<IEndOfLifeDateClient>();
            mockEndoflifeDateClient
                .Setup(x => x.GetFullProductsAsync(default))
                .ReturnsAsync(new FullProductListResponse
                {
                    Total = 2,
                    Result = [productA, productB]
                });

            var provider = new EndofLifeDateProvider(mockEndoflifeDateClient.Object);
            var actual = await provider.GetAsync(
                fromInclusive: new(2199, 1, 1, 0, 0, 0),
                toInclusive: new(2199, 1, 6, 23, 59, 59));

            Assert.Collection(actual,
                item =>
                {
                    Assert.Equal(item.ProductName, productA.Label);
                    Assert.Equal(item.ProductVersion, productARelease.Name);
                    Assert.Equal(item.ProductUrl, $"{productA.Links.Html}#:~:text={Uri.EscapeDataString(productARelease.Label)}");
                    Assert.Equal(item.Eol, productARelease.EolFrom);
                },
                item =>
                {
                    Assert.Equal(item.ProductName, productB.Label);
                    Assert.Equal(item.ProductVersion, productBRelease.Name);
                    Assert.Equal(item.ProductUrl, $"{productB.Links.Html}#:~:text={Uri.EscapeDataString(productBRelease.Label)}");
                    Assert.Equal(item.Eol, productBRelease.EoasFrom);
                });
        }
    }
}
