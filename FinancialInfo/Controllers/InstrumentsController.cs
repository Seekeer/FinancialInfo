using FinancialInfo.FinancialData;
using Microsoft.AspNetCore.Mvc;

namespace FinancialInfo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InstrumentsController(FinancialDataRepo financialDataRepo, 
        ILogger<InstrumentsController> logger) : ControllerBase
    {

        [HttpGet("{name}")]
        public decimal Get(string name)
        {
            if (!financialDataRepo.GetAllInstruments().Contains(name))
            {
                var error = new ArgumentException(ErrorMessages.INSTRUMENT_NOT_FOUND_ERROR_MESSAGE);
                logger.LogError(error, "Get request for wrong instrument");
                throw error;
            }

            var price = financialDataRepo.GetPrice(name);

            return price;
        }

        [HttpGet("list")]
        public IEnumerable<string> GetList()
        {
            return financialDataRepo.GetAllInstruments();
        }
    }
}
