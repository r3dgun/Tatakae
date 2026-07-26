using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[PermissionChecker(PermissionIds.AdminCouponsView)]
public sealed class AdminCouponsController(IAdminCouponService coupons) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResultDto<IReadOnlyCollection<CouponDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto<IReadOnlyCollection<CouponDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultDto<IReadOnlyCollection<CouponDto>>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await coupons.GetAllAsync(cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto<CouponDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await coupons.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return NotFound(result);
    }

    [PermissionChecker(PermissionIds.AdminCouponsManage)]
    [HttpPost]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultDto<CouponDto>>> Create(
        [FromBody] AdminCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await coupons.CreateAsync(request, cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [PermissionChecker(PermissionIds.AdminCouponsManage)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto<CouponDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultDto<CouponDto>>> Update(
        Guid id,
        [FromBody] AdminCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await coupons.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [PermissionChecker(PermissionIds.AdminCouponsManage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultDto>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await coupons.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }
}
