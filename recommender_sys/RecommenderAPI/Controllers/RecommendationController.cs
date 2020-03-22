using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommenderRepository _recommenderRepo;

        public RecommendationController(IRecommenderRepository recommenderRepo)
        {
            _recommenderRepo = recommenderRepo ?? throw new System.ArgumentNullException(nameof(recommenderRepo));
        }

        /// <summary>
        /// Get box recommendations based on an internal algorithm.
        /// </summary>
        /// <param name="userId">The id of the user to get recommendations for.</param>
        /// <param name="limit">The maximum amount of boxes to fetch.</param>
        /// <returns>A list of recommended boxes.</returns>
        [HttpGet("api/recommendations")]
        public async Task<ActionResult<IEnumerable<Box>>> FetchRecommendationsAsync(
            [Required]string userId,
            [Required]uint limit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Ok(await _recommenderRepo.FetchRecommendationsAsync(userId, limit));
        }
    }
}