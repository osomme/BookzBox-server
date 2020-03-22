using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly IKey _apiKeyHandler;
        private readonly LikeRepository _likeRepo;

        public LikeController(IKey key)
        {
            _apiKeyHandler = key ?? throw new System.ArgumentNullException(nameof(key));
            _likeRepo = new LikeRepository();
        }

        /// <summary>Adds the passed box to the database.</summary>
        /// <param name="key">The API key.</param>
        /// <param name="userId">The id of the user who likes the box.</param>
        /// <param name="boxId">The id of the box to like.</param> 
        /// <returns>
        ///     <see cref="BadRequestResult"/> if the passed data fails validation.
        ///     <see cref="ForbidResult"/> if the API key is not valid.
        ///     <see cref="OkResult"/> if successful.
        /// </returns>
        [HttpGet("api/like")]
        public async Task<IActionResult> LikeAsync(string key, string userId, string boxId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _likeRepo.LikeAsync(userId, boxId);

            return Ok();
        }
    }
}