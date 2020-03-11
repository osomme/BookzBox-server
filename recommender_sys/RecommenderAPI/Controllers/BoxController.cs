using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class BoxController : ControllerBase
    {

        private readonly BoxRepository _boxRepo;

        public BoxController()
        {
            _boxRepo = new BoxRepository(
                new UserRepository(),
                new BookRepository(new SubjectRepository())
            );
        }

        /// <summary>Adds the passed box to the database.</summary>
        /// <param name="key">The API key.</param>
        /// <param name="box">The box to add.</param>
        /// <returns>
        ///     <see cref="BadRequestResult"/> if the passed box fails validation.
        ///     <see cref="ForbidResult"/> if the API key is not valid.
        ///     <see cref="OkResult"/> if successful.
        /// </returns>
        [HttpPost("api/box")]
        public async Task<IActionResult> AddAsync([FromQuery] string key, [FromBody] Box box)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!ApiKey.IsValid(key))
            {
                return Forbid();
            }

            await _boxRepo.AddAsync(box);
            return Ok();
        }
    }
}