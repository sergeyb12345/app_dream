﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Dream.Space.Data.Entities.Strategies;
using Dream.Space.Data.Requests;
using Dream.Space.Data.Services;
using Dream.Space.Models.Strategies;

namespace Dream.Space.Controllers
{
    [RoutePrefix("api/strategy")]
    public class StrategiesApiController : ApiController
    {
        private readonly IStrategyService _service;

        public StrategiesApiController(IStrategyService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("getSummaries")]
        [ResponseType(typeof(List<StrategySummary>))]
        public async Task<IHttpActionResult> GeStrategySummaries()
        {
            var records = await _service.GetStrategiesAsync();
            return Ok(records);
        }

        [HttpGet]
        [Route("getByUrl/{url}")]
        [ResponseType(typeof(StrategyModel))]
        public async Task<IHttpActionResult> GetStrategyByUrl(string url)
        {
            var records = await _service.GetStrategyByUrlAsync(url);
            return Ok(records);
        }

        [HttpGet]
        [Route("getSummaryByUrl/{url}")]
        [ResponseType(typeof(StrategySummary))]
        public async Task<IHttpActionResult> GetStrategySummaryByUrl(string url)
        {
            var records = await _service.GetSummaryByUrlAsync(url);
            return Ok(records);
        }
        [HttpGet]
        [Route("get/{id:int:min(1)}")]
        [ResponseType(typeof(Strategy))]
        public async Task<IHttpActionResult> GetStrategyById(int id)
        {
            var records = await _service.GetStrategyAsync(id);
            return Ok(records);
        }


        [HttpPost]
        [Route("")]
        [ResponseType(typeof(StrategyModel))]
        public async Task<IHttpActionResult> SaveStrategy([FromBody] StrategyModel model)
        {

            var result = await _service.SaveStrategyAsync(model);

            return Ok(result);
        }

        [HttpDelete]
        [Route("{id:int:min(1)}")]
        public async Task<IHttpActionResult> DeleteStrategy(int id)
        {
            await _service.DeleteStrategyAsync(id);
            return Ok();
        }


        [HttpPost]
        [Route("validate-trade")]
        [ResponseType(typeof(ValidateTradeResult))]
        public async Task<IHttpActionResult> ValidateTrade([FromBody] ValidateTradeRequest model)
        {
            var result = await _service.ValidateTrade(model);
            return Ok(result);
        }
    }
}
