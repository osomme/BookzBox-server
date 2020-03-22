using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class PreferencesController : ControllerBase
    {
        private readonly IKey _apiKeyHandler;

        private readonly PreferencesRepository _preferencesRepo;

        public PreferencesController(IKey key)
        {
            _apiKeyHandler = key ?? throw new ArgumentNullException(nameof(key));
            _preferencesRepo = new PreferencesRepository(
                new UserRepository(),
                new SubjectRepository()
            );
        }

        /// <summary>Adds the given user preferences.</summary>
        /// <param name="key">The API key.</param>
        /// <param name="user">The id of the user whose preferences to update.</param>
        /// <param name="preferences">The preferences to set for the given user.</param>
        /// <returns>
        ///     <see cref="BadRequestResult"/> if the passed data fails validation.
        ///     <see cref="ForbidResult"/> if the API key is not valid.
        ///     <see cref="OkResult"/> if successful.
        /// </returns>
        [HttpPut("api/preferences")]
        public async Task<IActionResult> UpdatePreferencesAsync(
            [FromQuery] string key,
            [FromQuery][Required] string userId,
            [FromBody][Required] Preferences preferences)
        {
            if (!ModelState.IsValid || preferences.Subjects.Length < 1)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _preferencesRepo.UpdatePrefferedSubjectsAsync(userId, preferences.Subjects);

            return Ok();
        }
    }
}