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

        private readonly IPreferencesRepository _preferencesRepo;

        public PreferencesController(IKey key, IPreferencesRepository preferencesRepository)
        {
            _apiKeyHandler = key ?? throw new ArgumentNullException(nameof(key));
            _preferencesRepo = preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));
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
            [FromBody] Preferences preferences)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _preferencesRepo.UpdatePreferredSubjectsAsync(userId, SubjectMapper.ToStringArray(preferences?.Subjects));

            return Ok();
        }
    }
}