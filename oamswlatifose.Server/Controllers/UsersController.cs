using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.DTO.User;
using oamswlatifose.Server.Services;
using oamswlatifose.Server.Services.UserProvisioning.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// Account provisioning for Admin/HR. Gated on employee-management permissions, which both
    /// Admin and HR have (so both can add users); the basic "User" role cannot.
    ///
    /// <para>License: Proprietary software by Roberto V Ramirez Jr (robram3000@gmail.com).
    /// A valid license key is required after the 30-day trial. Day 31 and beyond will
    /// deny all requests until a license issued by robram3000@gmail.com is activated.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UsersController : BaseApiController
    {
        private readonly IUserProvisioningService _service;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserProvisioningService service, ILogger<UsersController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Lists user accounts (Admin/HR).</summary>
        [HttpGet]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<List<UserAccountSummaryDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List() => Ok(await _service.ListAsync());

        /// <summary>Role choices for the create form (Admin/HR).</summary>
        [HttpGet("roles")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<List<RoleOptionDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Roles() => Ok(await _service.GetRoleOptionsAsync());

        /// <summary>Updates an existing employee + linked login account (Admin/HR).</summary>
        [HttpPut("{id:int}")]
        [PermissionAuthorize("edit_employees")]
        [ProducesResponseType(typeof(ServiceResponse<UserAccountSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserAccountDTO dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (!result.IsSuccess)
                return BadRequest(result);
            _logger.LogInformation("User {Id} updated by {AdminId}", id, GetCurrentUserId());
            return Ok(result);
        }

        /// <summary>Creates an employee + linked login account (Admin/HR).</summary>
        [HttpPost]
        [PermissionAuthorize("edit_employees")]
        [ProducesResponseType(typeof(ServiceResponse<UserAccountSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateUserAccountDTO dto)
        {
            var result = await _service.CreateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result);

            _logger.LogInformation("User '{Username}' created by {UserId}", dto.Username, GetCurrentUserId());
            return Ok(result);
        }
    }
}
