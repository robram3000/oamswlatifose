using Microsoft.AspNetCore.Authorization;

namespace oamswlatifose.Server.Utilities.Security
{
    /// <summary>
    /// Custom authorization attribute for permission-based access control.
    /// Enables fine-grained authorization at the controller/action level based on
    /// specific permissions rather than just roles.
    /// 
    /// <para>Usage Examples:</para>
    /// <para>[PermissionAuthorize("view_employees")]</para>
    /// <para>[PermissionAuthorize("manage_users", "admin_access")]</para>
    /// </summary>
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "PERMISSION:";

        /// <summary>
        /// Initializes a new instance with required permissions.
        /// </summary>
        /// <param name="permissions">One or more permission names required for access</param>
        public PermissionAuthorizeAttribute(params string[] permissions)
        {
            Policy = $"{PolicyPrefix}{string.Join(",", permissions)}";
        }
    }

    /// <summary>
    /// Authorization handler that evaluates permission requirements against user claims.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var user = context.User;

            if (user == null)
                return Task.CompletedTask;

            // Check if user has all required permissions
            var hasAllPermissions = requirement.RequiredPermissions
                .All(permission => user.HasClaim("permission", permission));

            if (hasAllPermissions)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Authorization requirement for permission-based access control.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string[] RequiredPermissions { get; }

        public PermissionRequirement(string[] permissions)
        {
            RequiredPermissions = permissions;
        }
    }

    /// <summary>
    /// Policy provider that translates permission policies into requirements.
    /// </summary>
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        const string POLICY_PREFIX = PermissionAuthorizeAttribute.PolicyPrefix;

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy>(null);
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var permissions = policyName.Substring(POLICY_PREFIX.Length)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permissions))
                    .Build();

                return Task.FromResult(policy);
            }

            return Task.FromResult<AuthorizationPolicy>(null);
        }
    }
}
