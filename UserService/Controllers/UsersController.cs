﻿using System.Security.Claims;
using Core.Dtos;
using Core.Interfaces;
using Core.Models;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserService.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly IBlockService _blockService;
    private readonly IFollowerService _followerService;
    private readonly IPhotoService _photoService;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IBus _bus;
    private readonly IWebHostEnvironment _env;


    public UsersController(
        IUsersService usersService,
        IHttpContextAccessor httpContextAccessor,
        IFollowerService followerService,
        IBus bus,
        IWebHostEnvironment env,
        IBlockService blockService,
        IPhotoService photoService)
    {
        _photoService = photoService;
        _usersService = usersService;
        _httpContextAccessor = httpContextAccessor;
        _followerService = followerService;
        _bus = bus;
        _blockService = blockService;
        _env = env;
    }

    [HttpGet("{id}"), Authorize]
    public async Task<ActionResult<SearchedUserResponseDto?>> GetUserById(string id)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }
        User? user = await _usersService.GetUserAsync(id);
        if (user == null || user.DeletedAt != null || user.BlockedAt != null)
        {
            return NotFound();
        }
        var blocked = await _blockService.IsBlocked(userId, id);
        Console.WriteLine(blocked);
        if (blocked)
        {
            return NotFound();
        }

        SearchedUserResponseDto userResponseDto = user.AsDtoSearchedUser();
        userResponseDto.Followers = await _usersService.GetFollowerCount(id);
        userResponseDto.Following = await _usersService.GetFollowingCount(id);
        if (userId != null)
        {
            userResponseDto.IsFollowed = await _followerService.IsFollowing(userId, id);
        }
        return Ok(userResponseDto);
    }

    [HttpGet, Authorize(Roles = "admin")]
    public async Task<ActionResult<PaginatedUserResponseDto>> GetPaginatedUsers([FromQuery] int size = 20, [FromQuery] int page = 0)
    {
        return Ok(await _usersService.GetPaginatedUsers(size, page));
    }

    [HttpGet("may-follow"), Authorize]
    public async Task<ActionResult<List<UserResponseDto>>> MayFollowUser([FromQuery] int size = 20)
    {
        return Ok(await _usersService.MayFollowUser(size));
    }

    [HttpGet("follow-author"), Authorize]
    public async Task<ActionResult<List<UserResponseDto>>> FollowAuthor()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }
        return Ok(await _usersService.FollowAuthor(userId));
    }


    [HttpPut("edit"), Authorize]
    public async Task<ActionResult<UserEditResponseDto?>> UpdateUser([FromForm] UserEditDto userEditDto)
    {

        if (_httpContextAccessor.HttpContext != null)
        {
            User? user = await _usersService.GetUserAsync(userEditDto.Id);
            if (user == null || user.DeletedAt != null || user.BlockedAt != null)
            {
                return NotFound();
            }
            string? id = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string role = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "";

            if (id != user.Id && role != "admin")
            {
                return Unauthorized();
            }

            if (userEditDto.ProfilePicture != null)
            {

                PhotoUploadResult img = await _photoService.AddPhotoAsync(userEditDto.ProfilePicture);
                user.ProfilePictureUrl = img.Url;

            }

            if (userEditDto.CoverPicture != null)
            {
                PhotoUploadResult img = await _photoService.AddPhotoAsync(userEditDto.CoverPicture, 1000, 1000);
                user.CoverPictureUrl = img.Url;
            }

            user.UserName = userEditDto.UserName;
            user.Email = userEditDto.Email;
            user.FirstName = userEditDto.FirstName;
            user.LastName = userEditDto.LastName;
            user.Gender = userEditDto.Gender;
            user.DateOfBirth = userEditDto.DateOfBirth;
            user.UpdatedAt = DateTime.Now;
            user.Bio = userEditDto.Bio;
            user.Address = userEditDto.Address;

            Console.WriteLine(user.CoverPictureUrl);

            await _usersService.UpdateGetUserAsync(user.Id, user);
            // RabbitMQ Publish start

            CacheNotificationConsumerDto cacheNotificationConsumerDto = new CacheNotificationConsumerDto
            {
                Type = "Edit Profile",
                IsNotification = false,
                RefUserId = user.Id,
            };
            await _bus.Publish(cacheNotificationConsumerDto);

            // RabbitMQ Publish end
            return Ok(user.AsDtoEdit());
        }
        else
        {
            return Unauthorized();
        }
    }


    [HttpGet("current-user"), Authorize]
    public async Task<ActionResult<UserResponseDto?>> GetCurrentUser()
    {
        UserResponseDto? user = await _usersService.GetAuthUser();
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
