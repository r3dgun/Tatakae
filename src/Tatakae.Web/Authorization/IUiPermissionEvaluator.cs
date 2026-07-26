namespace Tatakae.Web.Authorization;

public interface IUiPermissionEvaluator
{
    Task<bool> HasAsync(string permission);
}
