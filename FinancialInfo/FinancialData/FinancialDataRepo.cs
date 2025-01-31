using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace FinancialInfo.FinancialData
{
    public interface FinancialDataRepo
    {
        IEnumerable<string> GetAllInstruments();
        decimal GetPrice(string name);
        void SetPrice(string instrument, decimal price);

        public event FinanceDataUpdatedEventHandler FinanceDataUpdated;
    }

    public delegate Task FinanceDataUpdatedEventHandler(object sender, FinanceDataEventArgs data);

    public class FinanceDataEventArgs : EventArgs
    {
        public string InstrumentName { get; set; }
        public decimal Price { get; set; }
    }

    public class FinanceDataConcurrentDictRepo : FinancialDataRepo
    {
        public FinanceDataConcurrentDictRepo(IOptions<AppConfig> appConfig)
        {
            _appConfig = appConfig;
            foreach (var item in appConfig.Value.Tiingo.TiingoCryptoConfig.Instruments)
                _instrumentPrices.TryAdd(item, 0);
            foreach (var item in appConfig.Value.Tiingo.TiingoForexConfig.Instruments)
                _instrumentPrices.TryAdd(item, 0);
        }

        public event FinanceDataUpdatedEventHandler FinanceDataUpdated;

        public IEnumerable<string> GetAllInstruments()
        {
            return _instrumentPrices.Keys;
        }

        public decimal GetPrice(string name)
        {
            if (_instrumentPrices.TryGetValue(name, out decimal price))
                return price;

            return 0;
        }

        public void SetPrice(string instrument, decimal price)
        {
            _instrumentPrices[instrument] = price;

            OnDataUpdated(new FinanceDataEventArgs { InstrumentName = instrument, Price = price });
        }

        protected virtual void OnDataUpdated(FinanceDataEventArgs e) => FinanceDataUpdated?.Invoke(this, e);

        private readonly ConcurrentDictionary<string, decimal> _instrumentPrices = new ConcurrentDictionary<string, decimal>();
        private readonly IOptions<AppConfig> _appConfig;
    }
}