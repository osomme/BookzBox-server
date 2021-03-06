using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IKey _apiKeyHandler;

        private readonly IUserRepository _userRepository;

        public UserController(IKey key, IUserRepository userRepo)
        {
            _apiKeyHandler = key ?? throw new System.ArgumentNullException(nameof(key));
            _userRepository = userRepo ?? throw new System.ArgumentNullException(nameof(userRepo));
        }

        /// <summary>Adds the passed user to the database.</summary>
        /// <param name="key">The API key.null</param>
        /// <param name="user">The user to add.</param>
        /// <returns>
        ///     <see cref="BadRequestResult"/> if the passed user fails validation.
        ///     <see cref="ForbidResult"/> if the API key is not valid.
        ///     <see cref="OkResult"/> if successful.
        /// </returns>
        [HttpPost("api/users")]
        public async Task<IActionResult> AddAsync([FromQuery] string key, [FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _userRepository.AddAsync(user);

            return Ok();
        }

    }
}