using EolBot.Services.Report;
using EolBot.Services.Report.EndoflifeDate;

namespace EolBot.Tests.Report
{
    public class EndoflifeDateDailyReportTests
    {
        [Fact]
        public void Create_ReturnsNotEmptyReport_WhenTwoMatchingItemsProvided()
        {
            var report = new EndoflifeDateDailyReport();
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
                End-of-life (EOL) calendar for the next 7 days:

                Tue, 01 Jan:
                — <a href="https://endoflife.date/product-a"><b>Product A 1.0</b></a>
                
                Wed, 02 Jan:
                None
                
                Thu, 03 Jan:
                None
                
                Fri, 04 Jan:
                None
                
                Sat, 05 Jan:
                None
                
                Sun, 06 Jan:
                — <a href="https://endoflife.date/product-b"><b>Product B 2.0</b></a>
                
                Mon, 07 Jan:
                None

                <i>Source: https://github.com/endoflife-date/release-data</i>
                """, actual);
        }

        [Fact]
        public void Create_ReturnsEmptyReport_WhenNoItemsProvided()
        {
            var report = new EndoflifeDateDailyReport();
            var from = new DateTime(2199, 1, 1, 0, 0, 0);
            var to = new DateTime(2199, 1, 7, 0, 0, 0);
            var actual = report.Create(from, to, []);

            Assert.Equal("""
                End-of-life (EOL) calendar for the next 7 days:

                Tue, 01 Jan:
                None
                
                Wed, 02 Jan:
                None
                
                Thu, 03 Jan:
                None
                
                Fri, 04 Jan:
                None
                
                Sat, 05 Jan:
                None
                
                Sun, 06 Jan:
                None
                
                Mon, 07 Jan:
                None
                
                <i>Source: https://github.com/endoflife-date/release-data</i>
                """, actual);
        }
    }
}
