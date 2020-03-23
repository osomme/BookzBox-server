using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BookzBox.Controllers
{
    [ApiController]
    public class BoxController : ControllerBase
    {
        private readonly IKey _apiKeyHandler;

        private readonly IBoxRepository _boxRepo;

        public BoxController(IKey apiKeyHandler, IBoxRepository boxRepo)
        {
            _apiKeyHandler = apiKeyHandler ?? throw new ArgumentNullException(nameof(apiKeyHandler));
            _boxRepo = boxRepo ?? throw new ArgumentNullException(nameof(boxRepo));
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

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _boxRepo.AddAsync(box);
            return Ok();
        }

        [HttpPut("api/box/status")]
        public async Task<IActionResult> UpdateStatusAsync([FromQuery] string key, [Required] string boxId, [Required] int status)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _boxRepo.UpdateStatusAsync(boxId, (BoxStatus)status);
            return Ok();
        }

        /// <summary>Deletes a box.</summary>
        /// <param name="key">The API key.</param>
        /// <param name="boxId">Id of the box to delete.</param>
        /// <returns>
        ///     <see cref="BadRequestResult"/> if the passed boxId fails validation.
        ///     <see cref="ForbidResult"/> if the API key is not valid.
        ///     <see cref="OkResult"/> if successful.
        /// </returns>
        [HttpDelete("api/box")]
        public async Task<IActionResult> DeleteBoxAsync([FromQuery][Required] string key,
                                                        [FromQuery][Required] string boxId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_apiKeyHandler.IsValid(key))
            {
                return Forbid();
            }

            await _boxRepo.DeleteBoxAync(boxId);

            return Ok();
        }

    }
}