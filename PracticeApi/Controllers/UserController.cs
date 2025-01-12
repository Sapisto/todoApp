using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeApi.Core.UserServices;
using PracticeApi.Domain.DTO;
using PracticeApi.DTO.UserDTOs;

namespace PracticeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }
        //Register route
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequest userRegistrationRequest)
        {
            if (userRegistrationRequest == null)
            {
                return BadRequest(new GeneralResponse<object>
                {
                    Succeeded = false,
                    Code = 400,
                    Message = "Invalid user data."
                });
            }

            // Call the CreateUser method to handle user registration
            var response = await _userService.CreateUser(userRegistrationRequest);

            // Return the response directly
            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response with appropriate code
        }

        //Login Route
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest userLoginRequest)
        {
            if (userLoginRequest == null)
            {
                return BadRequest(new GeneralResponse<object>
                {
                    Succeeded = false,
                    Code = 400,
                    Message = "Invalid login data."
                });
            }

            // Call the LoginUser method to authenticate and generate token
            var response = await _userService.LoginUser(userLoginRequest);

            // Return the response directly
            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response with appropriate code
        }

        // Endpoint to get all users
        [HttpGet("get-all-users")]
        //[Authorize]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            // Call the service method to fetch paginated users
            var response = await _userService.GetAllUsers(pageNumber, pageSize);

            // Return the response directly
            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response with appropriate status code
        }


        // Endpoint to get user by Id
        [HttpGet("get-user-by-id/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var response = await _userService.GetUserById(userId);

            if (response.Succeeded)
            {
                return Ok(response);
            }
            else
            {
                return StatusCode(response.Code, response);
            }
        }

        // Endpoint to update user details by Id
        [HttpPut("update-user/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserUpdateRequest userUpdateRequest)
        {
            if (userUpdateRequest == null)
            {
                return BadRequest(new GeneralResponse<object>
                {
                    Succeeded = false,
                    Code = 400,
                    Message = "Invalid user data."
                });
            }

            // Call the UpdateUser method to update user details
            var response = await _userService.UpdateUser(userId, userUpdateRequest);

            // Return the response directly
            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response with appropriate status code
        }

        // Endpoint to delete user by Id
        [HttpDelete("delete-user/{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var response = await _userService.DeleteUser(userId);

            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }
            else
            {
                return StatusCode(response.Code, response); // Return error response with appropriate status code
            }
        }
    }
}
