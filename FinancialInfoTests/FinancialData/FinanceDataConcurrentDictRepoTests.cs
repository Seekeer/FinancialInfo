using Microsoft.Extensions.Options;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FinancialInfo.FinancialData.Tests
{
    [TestFixture()]
    public class FinanceDataConcurrentDictRepoTests
    {
        private const string BTC = "BTC";
        private const string USD = "USD";

        [Test()]
        public async Task GetAllInstruments()
        {
            var repo = CreateTestObj();

            Assert.IsTrue(repo.GetAllInstruments().Contains(BTC));
            Assert.IsTrue(repo.GetAllInstruments().Contains(USD));
        }

        [Test()]
        public async Task SetPriceEvent()
        {
            var repo = CreateTestObj();
            var price = 10;
            FinanceDataEventArgs args = null;

            repo.FinanceDataUpdated += async (sender, eventArgs) => { args = eventArgs; };

            repo.SetPrice(USD, 10);

            Assert.AreEqual(price, args.Price);
            Assert.AreEqual(USD, args.InstrumentName);
        }

        [Test()]
        public async Task GetPrice()
        {
            var repo = CreateTestObj();

            var usdPrice = 12;
            repo.SetPrice(USD, usdPrice);

            var btcPrice = 22;
            repo.SetPrice(BTC, btcPrice);

            Assert.AreEqual(usdPrice, repo.GetPrice(USD));
            Assert.AreEqual(btcPrice, repo.GetPrice(BTC));

            btcPrice = 14;
            repo.SetPrice(BTC, btcPrice);

            Assert.AreEqual(usdPrice, repo.GetPrice(USD));
            Assert.AreEqual(btcPrice, repo.GetPrice(BTC));
        }

        private FinanceDataConcurrentDictRepo CreateTestObj()
        {
            return new FinanceDataConcurrentDictRepo(Options.Create<AppConfig>(new AppConfig
            {
                Tiingo = new TiingoConfig
                {
                    TiingoCryptoConfig = new TiingoEndpointConfig
                    {
                        Instruments = [BTC]
                    },
                    TiingoForexConfig = new TiingoEndpointConfig
                    {
                        Instruments = [USD]
                    },
                }
            }));
        }
    }
}