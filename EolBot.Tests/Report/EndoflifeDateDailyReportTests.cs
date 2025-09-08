using EolBot.Services.Localization.Abstract;
using EolBot.Services.Report;
using EolBot.Services.Report.EndoflifeDate;
using Moq;

namespace EolBot.Tests.Report
{
    public class EndoflifeDateDailyReportTests
    {
        private readonly ILocalizationService _localizer;

        public EndoflifeDateDailyReportTests()
        {
            var mockLocalizer = new Mock<ILocalizationService>();
            mockLocalizer.Setup(x => x.GetString("ReportHeader", null))
                .Returns("Products whose support ends in the next 7 days");
            mockLocalizer.Setup(x => x.GetString("None", null))
                .Returns("None");
            mockLocalizer.Setup(x => x.GetString("Source", null))
                .Returns("Source");
            _localizer = mockLocalizer.Object;
        }

        [Fact]
        public void Create_ReturnsNotEmptyReport_WhenTwoMatchingItemsProvided()
        {
            var report = new EndoflifeDateDailyReport(_localizer);
            var from = new DateTime(2199, 1, 1, 0, 0, 0);
            var to = new DateTime(2199, 1, 7, 0, 0, 0);
            var actual = report.Create(from, to, [
                new ReportItem
                {
                    Eol = new DateTime(2199, 1, 1, 0, 0, 0),
                    ProductName = "Product A",
                    ProductVersion = "1.0",
                    ProductUrl = "https://endoflife.date/product-a"
                },
                new ReportItem
                {
                    Eol = new DateTime(2199, 1, 6, 0, 0, 0),
                    ProductName = "Product B",
                    ProductVersion = "2.0",
                    ProductUrl = "https://endoflife.date/product-b"
                }
                ]);

            Assert.Equal("""
                Products whose support ends in the next 7 days:

                Tue, 1 Jan:
                — <a href="https://endoflife.date/product-a"><b>Product A 1.0</b></a>
                
                Wed, 2 Jan:
                None
                
                Thu, 3 Jan:
                None
                
                Fri, 4 Jan:
                None
                
                Sat, 5 Jan:
                None
                
                Sun, 6 Jan:
                — <a href="https://endoflife.date/product-b"><b>Product B 2.0</b></a>
                
                Mon, 7 Jan:
                None

                <i>Source: https://endoflife.date</i>
                """, actual);
        }

        [Fact]
        public void Create_ReturnsEmptyReport_WhenNoItemsProvided()
        {
            var report = new EndoflifeDateDailyReport(_localizer);
            var from = new DateTime(2199, 1, 1, 0, 0, 0);
            var to = new DateTime(2199, 1, 7, 0, 0, 0);
            var actual = report.Create(from, to, []);

            Assert.Equal("""
                Products whose support ends in the next 7 days:

                Tue, 1 Jan:
                None
                
                Wed, 2 Jan:
                None
                
                Thu, 3 Jan:
                None
                
                Fri, 4 Jan:
                None
                
                Sat, 5 Jan:
                None
                
                Sun, 6 Jan:
                None
                
                Mon, 7 Jan:
                None
                
                <i>Source: https://endoflife.date</i>
                """, actual);
        }
    }
}
