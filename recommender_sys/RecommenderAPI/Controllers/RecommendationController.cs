using System.Collections.Generic;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly RecommenderRepository _recommenderRepo;

        public RecommendationController()
        {
            _recommenderRepo = new RecommenderRepository(new BoxRecordMapper());
        }

        [HttpGet("api/recommendations")]
        public async Task<ActionResult<IEnumerable<Box>>> FetchRecommendationsAsync(string userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Ok(await _recommenderRepo.FetchRecommendationsAsync(userId));
        }
    }
}