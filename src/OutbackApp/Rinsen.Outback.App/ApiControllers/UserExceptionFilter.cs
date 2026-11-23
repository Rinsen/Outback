using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Exceptions;

namespace Rinsen.Outback.App.ApiControllers;

/// <summary>
/// Maps the domain exceptions thrown by IUserService to API responses:
/// guard-rail violations (self protection, last admin) and duplicate
/// identities/login ids become 400 with a { detail } body, missing users
/// become 404. The detail field is what js/core/http.js surfaces to the user.
/// </summary>
public class UserExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case SelfProtectionException selfProtection:
                context.Result = new BadRequestObjectResult(new { detail = selfProtection.Message });
                context.ExceptionHandled = true;
                break;

            case LastAdminException lastAdmin:
                context.Result = new BadRequestObjectResult(new { detail = lastAdmin.Message });
                context.ExceptionHandled = true;
                break;

            case UserNotFoundException userNotFound:
                context.Result = new NotFoundObjectResult(new { detail = userNotFound.Message });
                context.ExceptionHandled = true;
                break;

            case IdentityAlreadyExistException identityExists:
                context.Result = new BadRequestObjectResult(new { detail = identityExists.Message });
                context.ExceptionHandled = true;
                break;

            case LocalAccountAlreadyExistException accountExists:
                context.Result = new BadRequestObjectResult(new { detail = accountExists.Message });
                context.ExceptionHandled = true;
                break;
        }
    }
}
