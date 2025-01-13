using PracticeApi.DTO.UserDTOs;
using PracticeApi.Persistent;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PracticeApi.Domain.DTO;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using PracticeApi.Core.Constants;

namespace PracticeApi.Core.UserServices
{
    public class UserService
    {
        private readonly DataContext _context;
        private readonly string _jwtSecret;


        // Constructor accepts secret for JWT token
        public UserService(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _jwtSecret = configuration.GetValue<string>("JWT:Secret");
        }

        // Register new user
        public async Task<GeneralResponse<UserRegistrationResponse>> CreateUser(UserRegistrationRequest request)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.PhoneNumber))
            {
                return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.RequiredFields, 400, false);
            }

            // Validate email format
            if (!Helpers.ValidationHelpers.IsValidEmail(request.Email))
            {
                return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.InvalidEmailFormat, 400, false);
            }

            // Validate password format
            if (!Helpers.ValidationHelpers.IsValidPassword(request.Password))
            {
                return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.InvalidPasswordFormat, 400, false);
            }

            // Check if passwords match
            if (request.Password != request.ConfirmPassword)
            {
                return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.PasswordsDoNotMatch, 400, false);
            }

            // Check for duplicate email
            var existingEmailUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingEmailUser != null)
            {
                return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.EmailAlreadyInUse, 409, false);
            }

            // Check if username is already in use
            var existingUsernameUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (existingUsernameUser != null)
            {
            return new GeneralResponse<UserRegistrationResponse>(Constants.ErrorMessages.UsernameAlreadyInUse, 409, false);
            }

            // Hash the password
            string hashedPassword = HashPassword(request.Password);

            // Create the new user
            var newUser = new Domain.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            // Save the new user to the database
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Create response object
            var userResponse = new UserRegistrationResponse
            {
                Id = newUser.Id,
                Name = newUser.Name,
                UserName = newUser.UserName,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                CreatedAt = newUser.CreatedAt
            };

            return new GeneralResponse<UserRegistrationResponse>(userResponse, "User Registered Successfully" );
        }


        // Get all users
        public async Task<PagedResponse<List<GetAllUsersDto>>> GetAllUsers(int pageNumber, int pageSize)
        {
            var usersQuery = _context.Users.Select(u => new GetAllUsersDto
            {
                Id = u.Id,
                Name = u.Name,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt
            });

            var totalRecords = await usersQuery.CountAsync();
            var users = await usersQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResponse<List<GetAllUsersDto>>(users, pageNumber, pageSize, totalRecords, "Users retrieved successfully.");
        }

        // Get user by Id
        public async Task<GeneralResponse<GetAllUsersDto>> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<GetAllUsersDto>("User Id is required.", 400, false);
            }

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new GetAllUsersDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new GeneralResponse<GetAllUsersDto>("User not found.", 404, false);
            }

            return new GeneralResponse<GetAllUsersDto>(user, "User retrieved successfully.");
        }


        // Login user
        public async Task<GeneralResponse<object>> LoginUser(UserLoginRequest userLoginRequest)
        {
            if (string.IsNullOrEmpty(userLoginRequest.UserName) || string.IsNullOrEmpty(userLoginRequest.Password))
            {
                return new GeneralResponse<object>("Username and password are required.", 400, false);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userLoginRequest.UserName);
            if (existingUser == null)
            {
                return new GeneralResponse<object>("User not found.", 404, false);
            }

            var hashedPassword = HashPassword(userLoginRequest.Password);
            if (existingUser.Password != hashedPassword)
            {
                return new GeneralResponse<object>("Invalid credentials.", 401, false);
            }

            // Generate JWT token
            var token = GenerateJwtToken(existingUser);

            var result = new
            {
                User = new
                {
                    existingUser.Id,
                    existingUser.Name,
                    existingUser.UserName,
                    existingUser.Email,
                    existingUser.PhoneNumber,
                    existingUser.CreatedAt

                },
                Token = token
            };

            return new GeneralResponse<object>(result, "User logged in successfully.");
        }
        // Update user details by Id
        public async Task<GeneralResponse<UserRegistrationResponse>> UpdateUser(string userId, UserUpdateRequest userUpdateRequest)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<UserRegistrationResponse>("User Id is required.", 400, false);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
            {
                return new GeneralResponse<UserRegistrationResponse>("User not found.", 404, false);
            }

            // Update user details
            existingUser.Name = userUpdateRequest.Name ?? existingUser.Name;
            existingUser.Email = userUpdateRequest.Email ?? existingUser.Email;
            existingUser.PhoneNumber = userUpdateRequest.PhoneNumber ?? existingUser.PhoneNumber;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            // Map to response DTO
            var updatedUserResponse = new UserRegistrationResponse
            {
                Id = existingUser.Id,
                Name = existingUser.Name,
                UserName = existingUser.UserName,
                Email = existingUser.Email,
                PhoneNumber = existingUser.PhoneNumber,
                CreatedAt = existingUser.CreatedAt

            };

            return new GeneralResponse<UserRegistrationResponse>(updatedUserResponse, "User updated successfully.");
        }

        // UserService class - adding DeleteUser method
        public async Task<GeneralResponse<object>> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<object>("User Id is required.", 400, false);
            }

            // Find the user in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new GeneralResponse<object>("User not found.", 404, false);
            }

            // Remove the user from the database
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return new GeneralResponse<object>("User deleted successfully.");
        }

        // Logout user
        public async Task<GeneralResponse<object>> Logout(HttpContext httpContext)
        {
            // Get the token from the Authorization header
            var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return new GeneralResponse<object>("No token provided, Please.", 401, false);
            }

            try
            {
                // Validate the token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken == null || jsonToken.ValidTo < DateTime.UtcNow)
                {
                    return new GeneralResponse<object>("Invalid or expired token.", 401, false);
                }

                // Return success response
                return new GeneralResponse<object>("User Logout  successful.", 200, true);
            }
            catch (Exception)
            {
                return new GeneralResponse<object>("Invalid token format.", 401, false);
            }
        }

        // Change user password
        public async Task<GeneralResponse<object>> ChangePassword(string userId, ChangePasswordRequest changePasswordRequest)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<object>("User ID is required.", 400, false);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new GeneralResponse<object>("User not found.", 404, false);
            }

            // Validate old password
            var hashedCurrentPassword = HashPassword(changePasswordRequest.CurrentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                return new GeneralResponse<object>("Current password is incorrect.", 400, false);
            }

            // Password complexity validation
            string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            if (!Regex.IsMatch(changePasswordRequest.NewPassword, passwordRegex))
            {
                return new GeneralResponse<object>("New password must contain at least one uppercase letter, Lowercase, special character", 400, false);
            }

            // Hash the new password
            string hashedNewPassword = HashPassword(changePasswordRequest.NewPassword);

            // Update the user's password
            user.Password = hashedNewPassword;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new GeneralResponse<object>("Password changed successfully.", 200, true);
        }






        // Hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }


        // Generate JWT token
        private string GenerateJwtToken(Domain.Entities.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "Acube",
                audience: "Acube",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), //Token expiration
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
