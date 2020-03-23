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
        /// Get box recommendations based on an internal algorithm using the location
        /// given. 
        /// </summary>
        /// <param name="userId">The id of the user to get recommendations for.</param>
        /// <param name="latitude">
        /// The latitude coordinate of the location to get recommendations for.
        /// This value should be < 0 (negative) if no location data is provided.
        /// </param>
        /// <param name="longitude">
        /// The longitude coordinate of the location to get recommendations for.
        /// This value should be < 0 (negative) if no location data is provided.
        /// </param>
        /// <param name="limit">The maximum amount of boxes to fetch.</param>
        /// <returns>A list of recommended boxes.</returns>
        [HttpGet("api/recommendations")]
        public async Task<ActionResult<IEnumerable<Box>>> FetchRecommendationsAsync(
            [Required]string userId,
            [Required]double latitude,
            [Required]double longitude,
            [Required]int limit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Ok(await _recommenderRepo.FetchRecommendationsAsync(userId, limit, latitude, longitude));
        }
    }
}