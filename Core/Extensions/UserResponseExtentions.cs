using Core.Dtos;
using Core.Models;

public static class Extensions
{
    public static UserResponseDto AsDto(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CoverPictureUrl = user.CoverPictureUrl,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            Address = user.Address,
            Bio = user.Bio
        };
    }
    public static UserEditResponseDto AsDtoEdit(this User user)
    {
        return new UserEditResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CoverPictureUrl = user.CoverPictureUrl,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            Address = user.Address,
            Bio = user.Bio
        };
    }

    public static SearchedUserResponseDto AsDtoSearchedUser(this User user)
    {
        return new SearchedUserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CoverPictureUrl = user.CoverPictureUrl,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            Address = user.Address,
            Bio = user.Bio
        };
    }


}