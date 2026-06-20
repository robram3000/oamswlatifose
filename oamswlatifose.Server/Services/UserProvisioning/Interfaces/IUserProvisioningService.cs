using oamswlatifose.Server.DTO.User;

namespace oamswlatifose.Server.Services.UserProvisioning.Interfaces
{
    /// <summary>
    /// Lets Admin/HR create a login account (employee + EMAuthorizeruser) in one step and
    /// list existing accounts. Kept separate from the self-service AuthenticationService.Register.
    /// </summary>
    public interface IUserProvisioningService
    {
        Task<ServiceResponse<List<UserAccountSummaryDTO>>> ListAsync();
        Task<ServiceResponse<UserAccountSummaryDTO>> CreateAsync(CreateUserAccountDTO dto);
        Task<ServiceResponse<List<RoleOptionDTO>>> GetRoleOptionsAsync();
    }
}
